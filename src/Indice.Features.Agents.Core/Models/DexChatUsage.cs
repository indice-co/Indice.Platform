using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Models;

/// <summary>Token usage for a single chat turn. Mirrors <see cref="UsageDetails"/>. Product counters (questions used/total) live on <see cref="DexChatResponse"/>, not in <see cref="AdditionalCounts"/>.</summary>
public class DexChatUsage
{
    /// <summary>Number of tokens in the prompt / input.</summary>
    public long? InputTokenCount { get; set; }

    /// <summary>Number of tokens in the completion / output.</summary>
    public long? OutputTokenCount { get; set; }

    /// <summary>Total number of tokens for the turn.</summary>
    public long? TotalTokenCount { get; set; }

    /// <summary>Questions used in this session so far, for a <c>used/total</c> display. <c>null</c> when the message limit is disabled.</summary>
    public long? QuestionsUsedCount { get; set; }

    /// <summary>Total questions allowed per session, for a <c>used/total</c> display. <c>null</c> when the message limit is disabled.</summary>
    public long? QuestionsLimitCount { get; set; }

}
