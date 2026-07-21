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
    public IReadOnlyList<Citation> Citations { get; init; } = [];

    /// <summary>Links to the source documents that were retrieved and used to compose the answer; empty for out-of-scope responses and on error.</summary>
    public IReadOnlyList<SourceDocumentLink> Sources { get; init; } = [];
}
