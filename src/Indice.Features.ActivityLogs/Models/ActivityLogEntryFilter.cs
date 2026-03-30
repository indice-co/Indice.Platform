namespace Indice.Features.ActivityLogs.Models;

/// <summary>Represents a base filter for an <see cref="ActivityLogEntry"/> query.</summary>
public class ActivityLogEntryFilterBase
{
    /// <summary>Period from.</summary>
    public DateTimeOffset? From { get; set; }
    /// <summary>Period to.</summary>
    public DateTimeOffset? To { get; set; }
    /// <summary>The unique identifier of the application.</summary>
    public string? ApplicationId { get; set; }
}

/// <summary>Represents a filter for an <see cref="ActivityLogEntry"/> query.</summary>
public class ActivityLogEntryFilter : ActivityLogEntryFilterBase
{
    /// <summary>The unique identifier of the subject.</summary>
    public string? Subject { get; set; }
    /// <summary>User's session id.</summary>
    public string? SessionId { get; set; }
    /// <summary>Indicates whether the specified log entry is marked for review.</summary>
    public bool? MarkForReview { get; set; }
    /// <summary>Indicates whether the specified log entry represents a successful attempt or not.</summary>
    public bool? Succeeded { get; set; }
    /// <summary>The name of the action.</summary>
    public string? ActionName { get; set; }
    /// <summary>The unique identifier of the resource.</summary>
    public string? ResourceId { get; set; }
    /// <summary>The type of the resource.</summary>
    public string? ResourceType { get; set; }
    /// <summary>The category of the action.</summary>
    public string? Category { get; set; }
}
