using IdentityServer4.Services;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

/// <summary>Enriches the sign in log entry with the session id (if applicable).</summary>
public sealed class SessionIdEnricher : ISignInLogEntryEnricher
{
    private readonly IUserSession _userSession;

    /// <summary>Creates a new instance of <see cref="SessionIdEnricher"/> class.</summary>
    /// <param name="userSession">Models a user's authentication session.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public SessionIdEnricher(IUserSession userSession) {
        _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
    }

    /// <inheritdoc />
    public int Order => 3;
    /// <inheritdoc />
    public SignInLogEnricherRunType RunType => SignInLogEnricherRunType.Synchronous;

    /// <inheritdoc />
    public async ValueTask EnrichAsync(SignInLogEntry logEntry) => logEntry.SessionId = await _userSession.GetSessionIdAsync();
}