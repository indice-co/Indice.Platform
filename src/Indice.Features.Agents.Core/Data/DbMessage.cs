using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Data;

/// <summary>A single turn (user or assistant) within a <see cref="DbSession"/>.</summary>
public class DbMessage
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key → <see cref="DbSession.Id"/>.</summary>
    public Guid SessionId { get; set; }

    /// <summary>Author role of this message. Persisted as the role's string value (e.g. <c>user</c>).</summary>
    public ChatRole Role { get; set; } = ChatRole.User;

    /// <summary>Message body.</summary>
    public List<AIContent> Contents { get; set; } = [];

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Prompt-token cost attributed to this turn (assistant rows only).</summary>
    public int? PromptTokens { get; set; }

    /// <summary>Completion-token cost attributed to this turn (assistant rows only).</summary>
    public int? CompletionTokens { get; set; }

    /// <summary>Model deployment that produced the assistant message.</summary>
    public string? ModelUsed { get; set; }

    /// <summary>Optional per-message metadata (JSON) — e.g. citations, retrieved candidate IDs, intent classification.</summary>
    public string? MetadataJson { get; set; }

    /// <summary>Navigation property to the citations associated with this session message.</summary>
    public ICollection<DbCitation> Citations { get; set; } = [];
}
