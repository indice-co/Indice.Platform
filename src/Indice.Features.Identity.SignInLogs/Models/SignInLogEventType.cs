namespace Indice.Features.Identity.SignInLogs.Models;

/// <summary>The type of event for sign in log.</summary>
public enum SignInLogEventType
{
    /// <summary>A token event occurred.</summary>
    TokenIssued,
    /// <summary>A user performed a full login.</summary>
    UserLoginCompleted,
    /// <summary>A user performed a (possibly) partial login in the system.</summary>
    UserPasswordLoginCompleted
}
