using Indice.Features.Identity.Core;

namespace Indice.Features.Identity.SignInLogs.Models;

/// <summary>Additional information about the user's sign in log entry.</summary>
public class SignInLogEntryExtraData
{
    /// <summary>Gets the server process identifier.</summary>
    public int ProcessId { get; set; }
    /// <summary>Gets the redirect URI.</summary>
    public string RedirectUri { get; set; }
    /// <summary>Gets the requested scopes.</summary>
    public string Scope { get; set; }
    /// <summary>Gets the tokens.</summary>
    public IEnumerable<SignInLogEntryToken> Tokens { get; set; }
    /// <summary>Gets the error.</summary>
    public string Error { get; set; }
    /// <summary>Gets the error description.</summary>
    public string ErrorDescription { get; set; }
    /// <summary>Gets the provider.</summary>
    public string Provider { get; set; }
    /// <summary></summary>
    public SignInLogEntryDevice Device { get; set; }
    /// <summary>User devices representation.</summary>
    public SignInLogEntryUserDevice UserDevice { get; set; }
    /// <summary>Describes a warning that may occur during a sign in event.</summary>
    public SignInWarning? Warning { get; set; }
    /// <summary>The name of the original event occurred.</summary>
    public string OriginalEventType { get; set; }
}
