using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows.Abstractions;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows;

/// <summary>
/// The output of <see cref="IDexRunner.RunAsync"/>. Projected by <c>DexRunner</c> from the final
/// envelope's payload, the workflow's failure events, and the accumulated token usage.
/// </summary>
public class RagResult
{
    /// <summary>The pipeline's answer — grounded when intent was in-scope; the polite refusal text from <c>OutOfScopeResponder</c> when not; <c>null</c> only when a step threw (<see cref="Failed"/>).</summary>
    public string? Answer { get; init; }

    /// <summary>Citations accumulated across retrieval/rerank/compose, surfaced from the final payload.</summary>
    public IReadOnlyList<Citation> Citations { get; init; } = [];

    /// <summary>Links to the source documents that were retrieved and used to compose the answer, surfaced from the final payload.</summary>
    public IReadOnlyList<SourceDocumentLink> Sources { get; init; } = [];

    /// <summary>True when a step threw and the workflow halted (surfaced via MAF's <c>ExecutorFailedEvent</c>). Out-of-scope is NOT a failure — it flows through <c>OutOfScopeResponder</c> and produces a regular <see cref="Answer"/>.</summary>
    public bool Failed { get; init; }

    /// <summary>Error message from the step that threw, prefixed with its executor id; <c>null</c> when <see cref="Failed"/> is false.</summary>
    public string? FailureReason { get; init; }

    /// <summary>Total reasoning-model token usage across this run, folded from the steps' <c>UsageEvent</c>s. Persisted to the session, not returned to the caller; <c>null</c> when no reasoning call ran.</summary>
    public UsageDetails? Usage { get; init; }

    /// <summary>The reasoning-model deployment the tokens were billed against; <c>null</c> when no reasoning call ran.</summary>
    public string? ModelUsed { get; init; }
}
