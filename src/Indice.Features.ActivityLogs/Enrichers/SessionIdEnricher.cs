#if NET9_0_OR_GREATER
using Duende.IdentityServer.Services;
#else
using IdentityServer4.Services;
#endif
using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs.Enrichers;

/// <summary>Enriches the activity log entry with the session id (if applicable).</summary>
public sealed class SessionIdEnricher : IActivityLogEntryEnricher
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
    public ActivityLogEnricherRunType RunType => ActivityLogEnricherRunType.Synchronous;

    /// <inheritdoc />
    public async ValueTask EnrichAsync(ActivityLogEntry logEntry) => logEntry.SessionId ??= await _userSession.GetSessionIdAsync();
}