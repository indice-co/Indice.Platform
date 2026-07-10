using Indice.Features.Agents.Core.Models;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows;

/// <summary>
/// Base type for the real-time events <see cref="Abstractions.IDexRunner.RunStreamingAsync"/> yields as
/// the pipeline executes: per-step progress (<see cref="DexStepEvent"/>), answer text deltas
/// (<see cref="DexDeltaEvent"/>), and a single terminal <see cref="DexFinalEvent"/>.
/// </summary>
public abstract class DexStreamEvent
{
    /// <summary>Timestamp of when the event was created.</summary>
    public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Signals that a pipeline step has started executing.</summary>
public sealed class DexStepEvent : DexStreamEvent
{
    /// <summary>Creates a new <see cref="DexStepEvent"/>.</summary>
    public DexStepEvent(string stepId, string label) {
        StepId = stepId;
        Label = label;
    }

    /// <summary>The executor id of the step (e.g. <c>Retriever</c>).</summary>
    public string StepId { get; }

    /// <summary>Human-friendly progress label for the step (e.g. <c>Retrieving relevant context</c>).</summary>
    public string Label { get; }
}

/// <summary>Carries a single incremental chunk of the answer as the composer streams it.</summary>
public sealed class DexDeltaEvent : DexStreamEvent
{
    /// <summary>Creates a new <see cref="DexDeltaEvent"/>.</summary>
    public DexDeltaEvent(string text) => Text = text;

    /// <summary>The incremental answer text.</summary>
    public string Text { get; }
}

/// <summary>
/// Terminal event yielded once after the run completes. Mirrors the fields of <see cref="RagResult"/>:
/// the full answer, citations, failure state, and reasoning-model token totals.
/// </summary>
public sealed class DexFinalEvent : DexStreamEvent
{
    /// <summary>The full grounded answer (or the out-of-scope refusal); <c>null</c> only when a step threw.</summary>
    public string? Answer { get; init; }

    /// <summary>Citations supporting the answer; empty for out-of-scope responses and on error.</summary>
    public IReadOnlyList<Citation> Citations { get; init; } = Array.Empty<Citation>();

    /// <summary>True when a step threw and the workflow halted. Out-of-scope is NOT a failure.</summary>
    public bool Failed { get; init; }

    /// <summary>Error message from the step that threw, prefixed with its executor id; <c>null</c> when not failed.</summary>
    public string? FailureReason { get; init; }

    /// <summary>Total reasoning-model token usage across this run; <c>null</c> when no reasoning call ran.</summary>
    public UsageDetails? Usage { get; init; }

    /// <summary>The reasoning-model deployment the tokens were billed against; <c>null</c> when no reasoning call ran.</summary>
    public string? ModelUsed { get; init; }
}
