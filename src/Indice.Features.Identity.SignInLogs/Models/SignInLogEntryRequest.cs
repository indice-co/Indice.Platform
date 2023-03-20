namespace Indice.Features.Identity.SignInLogs.Models;

/// <summary>Request model for updating a <see cref="SignInLogEntry"/> instance.</summary>
public class SignInLogEntryRequest
{
    /// <summary>Indicates whether we need to mark the specified log entry for review.</summary>
    public bool Review { get; set; }
    /// <summary>An optional comment when a log entry is marked for review.</summary>
    public string ReviewComment { get; set; }
}
