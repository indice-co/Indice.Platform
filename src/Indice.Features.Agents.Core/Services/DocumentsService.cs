using System.Linq.Expressions;
using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows;
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
            Blob = new DbDocumentBlob {
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
        return await _db.Set<DbChunk>()
            // placeholder for 
            //.Where(c => filters.Category == null || c.Category == null || c.Category == filters.Category)
            //.Where(c => filters.Language == null || c.Language == null || c.Language == filters.Language)
            .OrderBy(c => EF.Functions.VectorDistance("cosine", c.Embedding, sqlVector))
            .Take(topK)
            .Select(c => new RetrievedChunk {
                Id = c.Id,
                Source = new SourceDocumentLink {
                    Id = c.DocumentId,
                    SourceTitle = c.Document.Title,
                    SourceUrl = _sourceLinkGenerator.GenerateLink(c.Document.Source),
                    IsPrivate = c.Document.IsPrivate,
                    ContentHash = c.Document.ContentHash,
                    ContentType = c.Document.Blob == null ? "application/markdown" : c.Document.Blob.ContentType,
                    Length = c.Document.Blob == null ? -1 : c.Document.Blob.ContentLength,
                    FileName = c.Document.Blob == null ? c.Document.Title : c.Document.Blob.FileName,
                },
                Title = c.Title,
                HeadingPath = c.HeadingPath,
                Content = c.Content,
                TokenCount = c.TokenCount,
                Score = 1 - EF.Functions.VectorDistance("cosine", c.Embedding, sqlVector),
            })
            .Where(x => x.Score > minScore)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ClearAsync() {

        await _db.Set<DbChunk>().ExecuteDeleteAsync();
        await _db.Set<DbDocument>().ExecuteDeleteAsync();
    }

}
