using IdentityServer4.Services;
using Indice.Features.Identity.Core.Logging.Models;

namespace Indice.Features.Identity.Core.Logging.Enrichers;

internal class SessionIdEnricher : ISignInLogEntryEnricher
{
    private readonly IUserSession _userSession;

    public SessionIdEnricher(IUserSession userSession) {
        _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
    }

    public async Task Enrich(SignInLogEntry logEntry) {
        logEntry.SessionId = await _userSession.GetSessionIdAsync();
    }
}
