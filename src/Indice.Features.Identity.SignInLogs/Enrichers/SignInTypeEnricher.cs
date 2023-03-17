using System.Reflection;
using IdentityServer4;
using IdentityServer4.Models;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

internal class SignInTypeEnricher : ISignInLogEntryEnricher
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
        CustomGrantTypes.OtpAuthenticate,
        GrantType.ClientCredentials,
        IdentityServerConstants.PersistedGrantTypes.RefreshToken,
        TotpConstants.GrantType.Totp
    };

    public int Order => 2;

    public Task Enrich(SignInLogEntry logEntry) {
        if (logEntry?.SignInType is not null) {
            return Task.CompletedTask;
        }
        var extraData = logEntry?.ExtraData;
        var containsGrantTypeProperty = extraData is not null && ((IEnumerable<PropertyInfo>)extraData.GetType().DeclaredProperties).Where(x => x.Name == "GrantType").Any();
        if (!containsGrantTypeProperty) {
            return Task.CompletedTask;
        }
        var grantType = (string)extraData?.GrantType;
        if (INTERACTIVE_GRANT_TYPES.Contains(grantType)) {
            logEntry.SignInType = SignInType.Interactive;
        }
        if (NON_INTERACTIVE_GRANT_TYPES.Contains(grantType)) {
            logEntry.SignInType = SignInType.NonInteractive;
        }
        return Task.CompletedTask;
    }
}
