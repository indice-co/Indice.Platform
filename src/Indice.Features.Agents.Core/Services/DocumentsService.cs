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
    private readonly string _embeddingModel;
    private readonly int _embeddingDimensions;

    /// <summary>Creates a new <see cref="DocumentsService"/>.</summary>
    public DocumentsService(AgentsDbContext db, IOptions<AgentsOptions> options) {
        _db = db;
        _embeddingModel = options.Value.AzureOpenAI.Deployments.Embedding ?? string.Empty;
        _embeddingDimensions = options.Value.AzureOpenAI.EmbeddingDimensions;
    }

    /// <inheritdoc/>
    public async Task<(Guid Id, string ContentHash)?> FindBySourceAsync(string source, CancellationToken cancellationToken) {
        var hit = await _db.Set<DbDocument>()
            .AsNoTracking()
            .Where(d => d.Source == source)
            .Select(d => new { d.Id, d.ContentHash })
            .FirstOrDefaultAsync(cancellationToken);
        return hit is null ? null : (hit.Id, hit.ContentHash);
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
                ChunkId = c.Id,
                DocumentId = c.DocumentId,
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
