#if NET9_0_OR_GREATER
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Indice.Events;
using Indice.Features.Identity.Core.Events;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.Core;

/// <summary>
/// Handles <see cref="UserLoginEvent"/> to enforce single active session per user.
/// Removes all existing sessions when a user logs in, except the current one.
/// </summary>
public class SingleSessionLoginEventHandler : IPlatformEventHandler<UserLoginEvent>
{
    private readonly IServerSideSessionStore _sessionStore;
    private readonly ISessionManagementService _sessionManagement;
    private readonly ILogger<SingleSessionLoginEventHandler> _logger;

    /// <summary>Creates a new instance of <see cref="SingleSessionLoginEventHandler"/>.</summary>
    public SingleSessionLoginEventHandler(
        IServerSideSessionStore sessionStore,
        ISessionManagementService sessionManagement,
        ILogger<SingleSessionLoginEventHandler> logger) {
        _sessionStore = sessionStore;
        _sessionManagement = sessionManagement;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Handle(UserLoginEvent @event, PlatformEventArgs args) {
        if (!@event.Succeeded) {
            return;
        }
        var subjectId = @event.User.Id;
        var currentSessionId = @event.SessionId;
        if (string.IsNullOrWhiteSpace(subjectId) || string.IsNullOrWhiteSpace(currentSessionId)) {
            return;
        }

        var sessions = await _sessionStore.GetSessionsAsync(
            new SessionFilter { SubjectId = subjectId },
            CancellationToken.None);

        var sessionsToRevoke = sessions
            .Where(s => currentSessionId is null || s.SessionId != currentSessionId)
            .ToList();

        if (sessionsToRevoke.Count == 0) {
            return;
        }
        foreach (var session in sessionsToRevoke) {
            _logger.LogInformation(
                "Revoking session {SessionId} for user {SubjectId} due to single-session enforcement.",
                session.SessionId, subjectId);

            // Create the context for removing this specific session
            var context = new RemoveSessionsContext {
                SubjectId = subjectId,
                SessionId = session.SessionId,
            };
            await _sessionManagement.RemoveSessionsAsync(context);
        }
    }
}
#endif