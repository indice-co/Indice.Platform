namespace Indice.Features.ActivityLogs.Models;

/// <summary>The type of event for activity log.</summary>
public enum ActivityLogEventType
{
    /// <summary>A token event occurred.</summary>
    TokenIssued,
    /// <summary>A user performed a full login.</summary>
    UserLoginCompleted,
    /// <summary>A user performed a (possibly) partial login in the system.</summary>
    UserPasswordValidationCompleted
}
