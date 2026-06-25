using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Core.Workflows;

/// <summary>
/// The canonical output of the last step of a Dex RAG pipeline. Carries the grounded answer and the
/// citations the answer was grounded against.
/// </summary>
public class RagPipelineOutput
{
    /// <summary>The grounded answer produced by the pipeline, or <c>null</c> if no answer was generated (e.g. early-exit).</summary>
    public string? Answer { get; init; }

    /// <summary>Citations the answer was grounded against, projected from the reranked candidates.</summary>
    public IReadOnlyList<Citation> Citations { get; init; } = Array.Empty<Citation>();
}
