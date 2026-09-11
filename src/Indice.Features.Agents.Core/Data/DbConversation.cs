using Indice.Features.Agents.Core.Models;
using static Indice.Features.Agents.Core.AgentsOptions;

namespace Indice.Features.Agents.Core.Data;

/// <summary>A chat session belonging to a single user. Aggregates <see cref="DbMessage"/> turns and running token totals.</summary>
public class DbConversation
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary><c>sub</c> jwt claim of the owning user.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Optional title (auto-generated from the first user message when <see cref="SessionOptions.TitleAutoGenerate"/> is true).</summary>
    public string? Title { get; set; }

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Timestamp of the most recent activity (last appended message).</summary>
    public DateTimeOffset LastActivityAt { get; set; }

    /// <summary>Cumulative prompt-token usage across all turns in this session.</summary>
    public long InputTokenCount { get; set; }

    /// <summary>Cumulative completion-token usage across all turns in this session.</summary>
    public long OutputTokenCount { get; set; }

    /// <summary>Number of persisted messages in this session. Each turn appends two rows (user + assistant).</summary>
    public int MessageCount { get; set; }

    /// <summary>Optional pinning of this session to the top of the user's list.</summary>
    public bool Pin { get; set; }

    /// <summary>When <c>true</c> the conversation cannot be continued; new turns are rejected.</summary>
    public bool ReadOnly { get; set; }

    /// <summary>When <c>true</c> the conversation is filtered out of all read paths (list, detail, history, load).</summary>
    public bool Hidden { get; set; }

    /// <summary>Optional per-session metadata (JSON) — e.g. default filters or language preferences.</summary>
    public string? MetadataJson { get; set; }

    /// <summary>Optional subject of the conversation. it grounds the workflow so it can skip discovering the purpose of the visit.</summary>
    public ChatTopic? Topic { get; set; }

    /// <summary>Navigation: messages belonging to this session.</summary>
    public ICollection<DbMessage> Messages { get; set; } = [];
}
