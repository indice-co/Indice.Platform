using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Models;

/// <summary>Token and question usage — for a single chat turn (on <see cref="DexChatResponse"/>) or cumulative across a session (on <see cref="DexConversation"/>). Mirrors <see cref="UsageDetails"/> token counts and adds the product question counters.</summary>
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
