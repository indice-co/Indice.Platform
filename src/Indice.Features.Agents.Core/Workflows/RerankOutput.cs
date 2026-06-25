namespace Indice.Features.Agents.Core.Workflows;

/// <summary>Output payload of <c>Reranker</c>.</summary>
public class RerankOutput
{
    /// <summary>The classified intent, forwarded from upstream.</summary>
    public Intent Intent { get; init; } = new();

    /// <summary>Top-N candidates reordered by reranker score; their <c>Score</c> reflects the rerank outcome.</summary>
    public IReadOnlyList<RetrievedChunk> RerankedCandidates { get; init; } = Array.Empty<RetrievedChunk>();
}
