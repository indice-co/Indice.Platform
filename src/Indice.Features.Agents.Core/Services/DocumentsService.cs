using System.Linq.Expressions;
using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows.Ingestion;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Services;

/// <inheritdoc/>
public class DocumentsService : IDocumentsService
{
    private readonly AgentsDbContext _db;
    private readonly ISourceLinkGenerator _sourceLinkGenerator;
    private readonly string _embeddingModel;
    private readonly int _embeddingDimensions;

    /// <summary>Creates a new <see cref="DocumentsService"/>.</summary>
    public DocumentsService(AgentsDbContext db, IOptions<AgentsOptions> options, ISourceLinkGenerator sourceLinkGenerator) {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _sourceLinkGenerator = sourceLinkGenerator ?? throw new ArgumentNullException(nameof(sourceLinkGenerator));
        _embeddingModel = options.Value.AzureOpenAI.Deployments.Embedding ?? string.Empty;
        _embeddingDimensions = options.Value.AzureOpenAI.EmbeddingDimensions;
    }

    /// <inheritdoc/>
    public async Task<SourceDocument?> FindBySourceAsync(string source, bool includeData, CancellationToken cancellationToken) {
        Expression<Func<DbDocument, bool>> predicate = d => d.Source == source;
        if (Guid.TryParse(source, out var documentId)) {
            predicate = d => d.Id == documentId;
        }
        var query = _db.Set<DbDocument>()
            .AsNoTracking()
            .Where(predicate)
            .Select(d => new SourceDocument {
                Id = d.Id,
                ContentHash = d.ContentHash,
                Source = d.Source,
                IsPrivate = d.IsPrivate,
                ContentType = d.Blob == null ? "application/markdown" : d.Blob.ContentType,
                ContentLength = d.Blob == null ? -1 : d.Blob.ContentLength,
                FileName = d.Blob == null ? d.Title : d.Blob.FileName,
                LastModified = d.Blob == null ? null : d.Blob.LastModified,
                Data = includeData && d.Blob != null ? d.Blob.Data : null
            });
        var hit = await query.FirstOrDefaultAsync(cancellationToken);
        return hit;
    }

    /// <inheritdoc/>
    public async Task<Guid> ReplaceAsync(Guid? existingDocumentId, IngestedDocument document, IReadOnlyList<EmbeddedChunk> chunks, CancellationToken cancellationToken) {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        if (existingDocumentId is Guid existingId) {
            await _db.Set<DbChunk>()
                .Where(c => c.DocumentId == existingId)
                .ExecuteDeleteAsync(cancellationToken);
            await _db.Set<DbDocument>()
                .Where(d => d.Id == existingId)
                .ExecuteDeleteAsync(cancellationToken);
        }
        var docId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        _db.Add(new DbDocument {
            Id = docId,
            Title = document.Title,
            Source = document.Source,
            Category = document.Category,
            Language = document.Language,
            ContentHash = document.ContentHash,
            EmbeddingModel = _embeddingModel,
            EmbeddingDimensions = _embeddingDimensions,
            Status = DocumentStatus.Ingested,
            ChunkCount = chunks.Count,
            IngestedAt = now,
            IsPrivate = document.IsPrivate,
            Blob = new DbBlob {
                ContentType = document.ContentType,
                ContentLength = document.ContentLength,
                FileName = document.FileName,
                Data = document.FileData,
                ETag = document.ContentHash,
            }
        });

        foreach (var ec in chunks) {
            _db.Add(new DbChunk {
                Id = Guid.NewGuid(),
                DocumentId = docId,
                Title = ec.Chunk.Title,
                Source = document.Source,
                ChunkIndex = ec.Chunk.ChunkIndex,
                Content = ec.Chunk.Content,
                ContentHash = ec.Chunk.ContentHash,
                Category = ec.Chunk.Category ?? document.Category,
                Language = document.Language,
                HeadingPath = ec.Chunk.HeadingPath,
                Embedding = new SqlVector<float>(ec.Embedding),
                EmbeddingModel = _embeddingModel,
                TokenCount = ec.Chunk.TokenCount,
                CreatedAt = now,
            });
        }
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return docId;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(ReadOnlyMemory<float> queryVector, RetrievalFilters filters, int topK, double minScore, CancellationToken cancellationToken) {
        var sqlVector = new SqlVector<float>(queryVector);
        var maxDistance = 1 - minScore;

        // CTE-based approach: CROSS APPLY for vector distance, then join only top K results
        var sql = @"
            ;WITH RankedChunks AS (
                SELECT TOP({0})
                    c.Id,
                    dist.Distance
                FROM [dex].[Chunk] c
                CROSS APPLY (
                    SELECT VECTOR_DISTANCE('cosine', c.Embedding, {1}) AS Distance
                ) dist
                WHERE dist.Distance <= {2}
                ORDER BY dist.Distance
            )
            SELECT 
                c.Id, 
                c.DocumentId, 
                c.HeadingPath, 
                c.Title, 
                c.Content, 
                c.TokenCount, 
                d.Title AS DocumentTitle, 
                d.Source AS DocumentSource, 
                d.IsPrivate AS DocumentIsPrivate, 
                d.ContentHash AS DocumentContentHash, 
                CASE
                    WHEN b.DocumentId IS NULL THEN N'application/markdown'
                    ELSE b.ContentType
                END AS BlobContentType, 
                CASE
                    WHEN b.DocumentId IS NULL THEN CAST(-1 AS bigint)
                    ELSE b.ContentLength
                END AS BlobLength, 
                CASE
                    WHEN b.DocumentId IS NULL THEN d.Title
                    ELSE b.FileName
                END AS BlobFileName,
                rc.Distance
            FROM [dex].[Chunk] AS c
            INNER JOIN RankedChunks AS rc ON c.Id = rc.Id
            INNER JOIN [dex].[Document] AS d ON c.DocumentId = d.Id
            LEFT JOIN [dex].[Blob] AS b ON d.Id = b.DocumentId
            ORDER BY rc.Distance";

        var rows = await _db.Database
            .SqlQueryRaw<SearchResultDto>(sql, topK, sqlVector, maxDistance)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Client-side transformation with SourceLinkGenerator
        return rows.Select(r => new RetrievedChunk {
            Id = r.Id,
            Source = new SourceDocumentLink {
                Id = r.DocumentId,
                SourceTitle = r.DocumentTitle,
                SourceUrl = _sourceLinkGenerator.GenerateLink(r.DocumentSource),
                IsPrivate = r.DocumentIsPrivate,
                ContentHash = r.DocumentContentHash,
                ContentType = r.BlobContentType,
                Length = r.BlobLength,
                FileName = r.BlobFileName,
            },
            Title = r.Title,
            HeadingPath = r.HeadingPath,
            Content = r.Content,
            TokenCount = r.TokenCount,
            Score = 1 - r.Distance,
        }).ToList();
    }

    // DTO for CTE query results
    private class SearchResultDto
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? HeadingPath { get; set; }
        public string Content { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;
        public string DocumentSource { get; set; } = string.Empty;
        public bool DocumentIsPrivate { get; set; }
        public string DocumentContentHash { get; set; } = string.Empty;
        public string BlobContentType { get; set; } = string.Empty;
        public long BlobLength { get; set; }
        public string BlobFileName { get; set; } = string.Empty;
        public double Distance { get; set; }
    }

    /// <inheritdoc/>
    public async Task ClearAsync() {

        await _db.Set<DbChunk>().ExecuteDeleteAsync();
        await _db.Set<DbDocument>().ExecuteDeleteAsync();
    }

}
