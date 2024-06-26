﻿using IdentityServer4;
using IdentityServer4.Models;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

/// <summary>Enriches the sign in log entry with the type of sign in (interactive vs non-interactive).</summary>
public sealed class SignInTypeEnricher : ISignInLogEntryEnricher
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
    public SignInLogEnricherRunType RunType => SignInLogEnricherRunType.Default;

    /// <inheritdoc />
    public ValueTask EnrichAsync(SignInLogEntry logEntry) {
        if (logEntry?.SignInType is not null || string.IsNullOrWhiteSpace(logEntry?.GrantType)) {
            return ValueTask.CompletedTask;
        }
        if (INTERACTIVE_GRANT_TYPES.Contains(logEntry.GrantType)) {
            logEntry.SignInType = SignInType.Interactive;
        }
        if (NON_INTERACTIVE_GRANT_TYPES.Contains(logEntry.GrantType)) {
            logEntry.SignInType = SignInType.NonInteractive;
        }
        return ValueTask.CompletedTask;
    }
}