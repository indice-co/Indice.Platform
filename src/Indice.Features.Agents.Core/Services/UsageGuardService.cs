using Indice.Features.Agents.Core.Models;
using Microsoft.Extensions.Options;
using static Indice.Features.Agents.Core.AgentsOptions;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// A service that provides usage guard functionality for AI agents.
/// This service is responsible for monitoring and controlling the usage of AI agents to ensure token limits are not exceeded and usage policies are adhered to.
/// </summary>
public class UsageGuardService : IUsageGuardService
{
    private readonly ISessionsStore _store;
    private readonly SessionOptions _sessionOptions;

    /// <summary>Creates a new <see cref="UsageGuardService"/>.</summary>
    public UsageGuardService(ISessionsStore store, IOptions<AgentsOptions> options) {
        _store = store;
        _sessionOptions = options.Value.Session;
    }

    /// <inheritdoc/>
    public UsageGuardResult Check(Session session) {
        if (_sessionOptions.MaxMessagesPerSession > 0 && session.MessageCount + 2 > _sessionOptions.MaxMessagesPerSession) {
            return UsageGuardResult.Deny(_sessionOptions.LimitReachedMessage);
        }
        if (_sessionOptions.MaxTokensPerSession > 0 && session.TotalPromptTokens + session.TotalCompletionTokens >= _sessionOptions.MaxTokensPerSession) {
            return UsageGuardResult.Deny(_sessionOptions.LimitReachedMessage);
        }
        return UsageGuardResult.Allow();
    }

    /// <inheritdoc/>
    public async Task<UsageGuardResult> CheckSessionCreationAsync(string userId, CancellationToken cancellationToken) {
        if (_sessionOptions.MaxSessionsPerUser > 0 && await _store.CountSessionsAsync(userId, cancellationToken) >= _sessionOptions.MaxSessionsPerUser) {
            return UsageGuardResult.Deny(_sessionOptions.MaxSessionsReachedMessage);
        }
        return UsageGuardResult.Allow();
    }
}
