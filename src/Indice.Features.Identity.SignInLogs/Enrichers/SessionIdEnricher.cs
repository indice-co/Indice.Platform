using IdentityServer4.Services;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

/// <summary></summary>
public sealed class SessionIdEnricher : ISignInLogEntryEnricher
{
    private readonly IUserSession _userSession;

    /// <summary></summary>
    /// <param name="userSession"></param>
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