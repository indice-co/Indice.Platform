#if NET9_0_OR_GREATER
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
#else
using IdentityServer4;
using IdentityServer4.Models;
#endif
using Indice.Features.Identity.Core;
using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs.Enrichers;

/// <summary>Enriches the activity log entry with the type of activity (interactive vs non-interactive).</summary>
public sealed class ActivityTypeEnricher : IActivityLogEntryEnricher
{
    private static readonly IReadOnlyList<string> INTERACTIVE_GRANT_TYPES = new List<string> {
        CustomGrantTypes.DeviceAuthentication,
        GrantType.AuthorizationCode,
        GrantType.DeviceFlow,
        GrantType.Hybrid,
        GrantType.Implicit,
        GrantType.ResourceOwnerPassword
    };

    private static readonly IReadOnlyList<string> NON_INTERACTIVE_GRANT_TYPES = new List<string> {
        CustomGrantTypes.Delegation,
        CustomGrantTypes.Mfa,
        GrantType.ClientCredentials,
        IdentityServerConstants.PersistedGrantTypes.RefreshToken,
        TotpConstants.GrantType.Totp
    };

    /// <inheritdoc />
    public int Order => 7;
    /// <inheritdoc />
    public ActivityLogEnricherRunType RunType => ActivityLogEnricherRunType.Default;

    /// <inheritdoc />
    public ValueTask EnrichAsync(ActivityLogEntry logEntry) {
        if (logEntry?.ActivityType is not null || string.IsNullOrWhiteSpace(logEntry?.GrantType)) {
            return ValueTask.CompletedTask;
        }
        if (INTERACTIVE_GRANT_TYPES.Contains(logEntry.GrantType)) {
            logEntry.ActivityType = ActivityType.Interactive;
        }
        if (NON_INTERACTIVE_GRANT_TYPES.Contains(logEntry.GrantType)) {
            logEntry.ActivityType = ActivityType.NonInteractive;
        }
        return ValueTask.CompletedTask;
    }
}