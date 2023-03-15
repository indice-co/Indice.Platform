namespace Indice.Features.Identity.SignInLogs.Models;

/// <summary>Request model for updating a <see cref="SignInLogEntry"/> instance.</summary>
public class SignInLogEntryRequest
{
    /// <summary>Indicates whether we need to mark the specified log entry for review.</summary>
    public bool MarkForReview { get; set; }
}
