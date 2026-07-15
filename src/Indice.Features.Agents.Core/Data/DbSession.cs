using static Indice.Features.Agents.Core.AgentsOptions;

namespace Indice.Features.Agents.Core.Data;

/// <summary>A chat session belonging to a single user. Aggregates <see cref="DbMessage"/> turns and running token totals.</summary>
public class DbSession
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Subject claim of the owning user.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Optional title (auto-generated from the first user message when <see cref="SessionOptions.TitleAutoGenerate"/> is true).</summary>
    public string? Title { get; set; }

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Timestamp of the most recent activity (last appended message).</summary>
    public DateTimeOffset LastActivityAt { get; set; }

    /// <summary>Cumulative prompt-token usage across all turns in this session.</summary>
    public long TotalPromptTokens { get; set; }

    /// <summary>Cumulative completion-token usage across all turns in this session.</summary>
    public long TotalCompletionTokens { get; set; }

    /// <summary>Number of persisted messages in this session. Each turn appends two rows (user + assistant).</summary>
    public int MessageCount { get; set; }

    /// <summary>Optional per-session metadata (JSON) — e.g. default filters or language preferences.</summary>
    public string? MetadataJson { get; set; }

    /// <summary>Navigation: messages belonging to this session.</summary>
    public ICollection<DbMessage> Messages { get; set; } = [];
}
