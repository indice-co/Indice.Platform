namespace Indice.Features.Agents.Core.Models;

/// <summary>Lightweight row used in paged session listings; messages are not included.</summary>
public class ConversationListItem
{
    /// <summary>Session identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Optional display title.</summary>
    public string? Title { get; init; }

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Timestamp of the most recent appended message.</summary>
    public DateTimeOffset LastActivityAt { get; init; }

    /// <summary>Cumulative prompt-token usage across all turns in this session.</summary>
    public long TotalPromptTokens { get; init; }

    /// <summary>Cumulative completion-token usage across all turns in this session.</summary>
    public long TotalCompletionTokens { get; init; }
}
