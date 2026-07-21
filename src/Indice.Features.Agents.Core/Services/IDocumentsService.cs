using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows;

namespace Indice.Features.Agents.Core.Services;

/// <summary>Persistence boundary for ingested documents. Only consumer of <see cref="Data.DbDocument"/> / <see cref="Data.DbChunk"/>.</summary>
public interface IDocumentsService
{
    /// <summary>Returns <c>SourceDocument</c> for the document with this <paramref name="source"/>, or <c>null</c> when none exists.</summary>
    Task<SourceDocument?> FindBySourceAsync(string source, bool includeData, CancellationToken cancellationToken);

    /// <summary>
    /// Atomic insert (and optional replace) in one transaction. When <paramref name="existingDocumentId"/> is
    /// supplied, its chunks and the document row are deleted first; then the new <see cref="IngestedDocument"/> +
    /// N <see cref="EmbeddedChunk"/>s are inserted. Returns the id of the newly inserted document.
    /// </summary>
    Task<Guid> ReplaceAsync(
        Guid? existingDocumentId,
        IngestedDocument document,
        IReadOnlyList<EmbeddedChunk> chunks,
        CancellationToken cancellationToken);

    /// <summary>
    /// Top-<paramref name="topK"/> chunks ordered by cosine similarity to <paramref name="queryVector"/>,
    /// filtered by category/language (null = no filter; chunks with NULL category/language always match).
    /// </summary>
    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(ReadOnlyMemory<float> queryVector, RetrievalFilters filters, int topK, double minScore, CancellationToken cancellationToken);

    ///<summary> Deletes all documents and chunks.</summary>
    Task ClearAsync();
}
