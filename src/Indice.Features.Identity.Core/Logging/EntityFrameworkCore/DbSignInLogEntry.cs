namespace Indice.Features.Identity.Core.Logging.EntityFrameworkCore;

/// <summary>A database entity representing a user's sign in log entry.</summary>
public class DbSignInLogEntry
{
    /// <summary>The unique id of the user's sign in log entry.</summary>
    public Guid Id { get; set; }
    /// <summary>A timestamp that indicates when the user's sign in log entry occurred.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>The source that generated the user's sign in log entry.</summary>
    public string Source { get; set; }
    /// <summary>The name of the action.</summary>
    public string ActionName { get; set; }
    /// <summary>The unique identifier of the subject (i.e. user id, client id).</summary>
    public string SubjectId { get; set; }
    /// <summary>The type of the subject (i.e. user, machine).</summary>
    public string SubjectType { get; set; }
    /// <summary>The display name of the subject.</summary>
    public string Subject { get; set; }
    /// <summary>The unique identifier of the resource.</summary>
    public string ResourceId { get; set; }
    /// <summary>The name of the resource.</summary>
    public string ResourceType { get; set; }
    /// <summary>A friendly text describing the log entry.</summary>
    public string Description { get; set; }
    /// <summary>Indicates whether the operation that caused the user's sign in log entry was successful or not.</summary>
    public bool Succedded { get; set; }
    /// <summary>Additional information about the user's sign in log entry.</summary>
    public dynamic ExtraData { get; set; }
}
