using Indice.Types;

namespace Indice.Features.Identity.SignInLogs.Models;

/// <summary>Represents a filter for an <see cref="SignInLogEntry"/> query.</summary>
public class SignInLogEntryFilter
{
    /// <summary>Period from.</summary>
    public DateTimeOffset? From { get; set; }
    /// <summary>Period to.</summary>
    public DateTimeOffset? To { get; set; }
    /// <summary>The unique identifier of the application.</summary>
    public string ApplicationId { get; set; }
    /// <summary>The unique identifier of the subject.</summary>
    public string SubjectId { get; set; }
    /// <summary>User's session id.</summary>
    public string SessionId { get; set; }
    /// <summary>Describes the user sign in type in terms of user presence.</summary>
    public SignInType? SignInType { get; set; }
    /// <summary>Indicates whether the specified log entry is marked for review.</summary>
    public bool? MarkForReview { get; set; }
}
