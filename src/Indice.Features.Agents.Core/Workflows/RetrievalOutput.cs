namespace Indice.Features.Agents.Core.Workflows;

/// <summary>Output payload of <c>Retriever</c>.</summary>
public class RetrievalOutput
{
    /// <summary>The classified intent, forwarded from upstream.</summary>
    public Intent Intent { get; init; } = new();

    /// <summary>Top candidates union'd and deduped across all rewritten queries, ordered by cosine similarity.</summary>
    public IReadOnlyList<RetrievedChunk> Candidates { get; init; } = Array.Empty<RetrievedChunk>();
}
