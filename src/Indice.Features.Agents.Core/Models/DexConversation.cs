namespace Indice.Features.Agents.Core.Models;

/// <summary>Detail view of a conversation, including the most recent messages.</summary>
public class DexConversation
{
    /// <summary>Conversation identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Optional display title; auto-generated from the first user message when the title-auto-generate option is enabled.</summary>
    public string? Title { get; init; }

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Timestamp of the most recent appended message.</summary>
    public DateTimeOffset LastActivityAt { get; init; }

    /// <summary>Number of persisted messages in this session. Each turn appends two rows (user + assistant).</summary>
    public int MessageCount { get; init; }

    /// <summary>Cumulative token and question usage across all turns in this session.</summary>
    public DexChatUsage Usage { get; init; } = new();

    /// <summary>Recent messages in chronological order (oldest first), capped at the configured history window.</summary>
    public IReadOnlyList<DexChatMessage> Messages { get; init; } = Array.Empty<DexChatMessage>();
}

