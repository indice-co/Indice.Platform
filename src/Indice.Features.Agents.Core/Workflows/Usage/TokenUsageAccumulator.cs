using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Usage;

/// <summary>
/// Request-scoped accumulator for token usage from the reasoning model. The reasoning chat client is
/// wrapped by <see cref="UsageTrackingChatClient"/>, which calls <see cref="Add"/> after each response;
/// <c>DexRunner</c> reads the totals once the pipeline completes so they can be persisted to the session.
/// Fast-model calls (query rewrite, rerank) are not wrapped and therefore not counted.
/// </summary>
public sealed class TokenUsageAccumulator
{
    private long _promptTokens;
    private long _completionTokens;

    /// <summary>Cumulative prompt (input) tokens across reasoning-model calls in this request.</summary>
    public long PromptTokens => Interlocked.Read(ref _promptTokens);

    /// <summary>Cumulative completion (output) tokens across reasoning-model calls in this request.</summary>
    public long CompletionTokens => Interlocked.Read(ref _completionTokens);

    /// <summary>The reasoning-model deployment the tokens were billed against; <c>null</c> until the first call.</summary>
    public string? Model { get; private set; }

    /// <summary>Adds the token counts from a reasoning-model response. No-op on the counts when <paramref name="usage"/> is <c>null</c>.</summary>
    public void Add(UsageDetails? usage, string model) {
        Model = model;
        if (usage is null) {
            return;
        }
        Interlocked.Add(ref _promptTokens, usage.InputTokenCount ?? 0);
        Interlocked.Add(ref _completionTokens, usage.OutputTokenCount ?? 0);
    }
}
