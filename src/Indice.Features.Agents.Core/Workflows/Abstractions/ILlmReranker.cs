namespace Indice.Features.Agents.Core.Workflows.Abstractions;

/// <summary>Reorders retrieval candidates by relevance to a question, trimming to a target size.</summary>
public interface ILlmReranker
{
    /// <summary>Rerank the supplied candidates and return the top N by descending relevance score.</summary>
    Task<IReadOnlyList<RetrievedChunk>> RerankAsync(string question, IReadOnlyList<RetrievedChunk> candidates, int topN, CancellationToken cancellationToken);
}
