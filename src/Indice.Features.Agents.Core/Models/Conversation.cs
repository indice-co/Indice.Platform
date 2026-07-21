using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Models;

/// <summary>Detail view of a conversation, including the most recent messages.</summary>
public class Conversation
{
    /// <summary>Conversation identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Optional display title; auto-generated from the first user message when the title-auto-generate option is enabled.</summary>
    public string? Title { get; init; }

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Timestamp of the most recent appended message.</summary>
    public DateTimeOffset LastActivityAt { get; init; }

    /// <summary>Cumulative prompt-token usage across all turns in this session.</summary>
    public long InputTokenCount { get; init; }

    /// <summary>Cumulative completion-token usage across all turns in this session.</summary>
    public long OutputTokenCount { get; init; }

    /// <summary>Number of persisted messages in this session. Each turn appends two rows (user + assistant).</summary>
    public int MessageCount { get; init; }

    /// <summary>Questions used in this session so far, for a <c>used/total</c> display. <c>null</c> when the message limit is disabled.</summary>
    public int? QuestionsUsedCount { get; init; }

    /// <summary>Total questions allowed per session, for a <c>used/total</c> display. <c>null</c> when the message limit is disabled.</summary>
    public int? QuestionsLimitCount { get; init; }

    /// <summary>Recent messages in chronological order (oldest first), capped at the configured history window.</summary>
    public IReadOnlyList<ChatMessage> Messages { get; init; } = Array.Empty<ChatMessage>();
}
