using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows.Abstractions;

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
    public IReadOnlyList<Citation> Citations { get; init; } = Array.Empty<Citation>();

    /// <summary>True when a step threw and the workflow halted (surfaced via MAF's <c>ExecutorFailedEvent</c>). Out-of-scope is NOT a failure — it flows through <c>OutOfScopeResponder</c> and produces a regular <see cref="Answer"/>.</summary>
    public bool Failed { get; init; }

    /// <summary>Error message from the step that threw, prefixed with its executor id; <c>null</c> when <see cref="Failed"/> is false.</summary>
    public string? FailureReason { get; init; }

    /// <summary>Prompt (input) tokens consumed by the reasoning model across this run. Captured by <c>UsageTrackingChatClient</c>; persisted to the session, not returned to the caller.</summary>
    public long PromptTokens { get; init; }

    /// <summary>Completion (output) tokens produced by the reasoning model across this run.</summary>
    public long CompletionTokens { get; init; }

    /// <summary>The reasoning-model deployment the tokens were billed against; <c>null</c> when no reasoning call ran.</summary>
    public string? ModelUsed { get; init; }
}
