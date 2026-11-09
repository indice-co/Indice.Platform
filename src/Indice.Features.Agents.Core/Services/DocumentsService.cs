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
        var category = filters.Category;
        var language = filters.Language;

        var candidates = _db.Set<DbChunk>().AsNoTracking();
        if (!string.IsNullOrEmpty(category)) {
            candidates = candidates.Where(c => c.Category == null || c.Category == category);
        }
        if (!string.IsNullOrEmpty(language)) {
            candidates = candidates.Where(c => c.Language == null || c.Language == language);
        }

        // Stage 1: rank by vector distance and keep only the top K ids.
        // Distinct() is a no-op on {Id, Distance} (Id is the PK) but it stops EF from pushing the projection
        // through the subquery, so Distance is materialized once as a column instead of VECTOR_DISTANCE
        // (and the Embedding column) being re-evaluated in the outer query.
        var ranked = candidates
            .Select(c => new { c.Id, Distance = EF.Functions.VectorDistance("cosine", c.Embedding, sqlVector) })
            .Where(x => x.Distance <= maxDistance)
            .OrderBy(x => x.Distance)
            .Take(topK)
            .Distinct();

        // Stage 2: join the K survivors back to chunk/document/blob metadata.
        var rows = await ranked
            .Join(_db.Set<DbChunk>(), r => r.Id, c => c.Id, (r, c) => new { r.Distance, Chunk = c })
            .OrderBy(x => x.Distance)
            .Select(x => new SearchResultDto {
                Id = x.Chunk.Id,
                DocumentId = x.Chunk.DocumentId,
                Title = x.Chunk.Title ?? string.Empty,
                HeadingPath = x.Chunk.HeadingPath,
                Content = x.Chunk.Content,
                TokenCount = x.Chunk.TokenCount,
                DocumentTitle = x.Chunk.Document.Title,
                DocumentSource = x.Chunk.Document.Source,
                DocumentIsPrivate = x.Chunk.Document.IsPrivate,
                DocumentContentHash = x.Chunk.Document.ContentHash,
                BlobContentType = x.Chunk.Document.Blob == null ? "application/markdown" : x.Chunk.Document.Blob.ContentType,
                BlobLength = x.Chunk.Document.Blob == null ? -1L : x.Chunk.Document.Blob.ContentLength,
                BlobFileName = x.Chunk.Document.Blob == null ? x.Chunk.Document.Title : x.Chunk.Document.Blob.FileName,
                Distance = x.Distance,
            })
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

    // Projection target for the two-stage vector search query
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
