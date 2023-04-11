using Indice.Features.Identity.SignInLogs.Models;
using Indice.Services;

namespace Indice.Features.Identity.SignInLogs.Events;

/// <summary>An event that is raised when a new <see cref="SignInLogEntry"/> is created.</summary>
public class SignInLogCreated : IPlatformEvent
{
    /// <summary>Creates a new instance of <see cref="SignInLogCreated"/> class.</summary>
    /// <param name="signInLog">The log entry that was created.</param>
    public SignInLogCreated(SignInLogEntry signInLog) {
        SignInLog = signInLog;
    }

    /// <summary>The log entry that was created.</summary>
    public SignInLogEntry SignInLog { get; set; }
}
