using IdentityServer4.Services;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

internal class SessionIdEnricher : ISignInLogEntryEnricher
{
    private readonly IUserSession _userSession;

    public SessionIdEnricher(IUserSession userSession) {
        _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
    }

    public int Order => 3;
    public SignInLogEnricherRunType RunType => SignInLogEnricherRunType.Synchronous;

    public async ValueTask EnrichAsync(SignInLogEntry logEntry) {
        logEntry.SessionId = await _userSession.GetSessionIdAsync();
    }
}
