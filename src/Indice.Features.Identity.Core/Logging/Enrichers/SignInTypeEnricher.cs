using System.Reflection;
using IdentityServer4.Models;
using Indice.Features.Identity.Core.Logging.Abstractions;
using Indice.Features.Identity.Core.Logging.Models;

namespace Indice.Features.Identity.Core.Logging.Enrichers;

internal class SignInTypeEnricher : ISignInLogEntryEnricher
{
    private static readonly IReadOnlyList<string> INTERACTIVE_GRANT_TYPES = new List<string> {
        CustomGrantTypes.DeviceAuthentication,
        GrantType.AuthorizationCode,
        GrantType.DeviceFlow,
        GrantType.Hybrid,
        GrantType.Implicit
    };

    private static readonly IReadOnlyList<string> NON_INTERACTIVE_GRANT_TYPES = new List<string> {
        CustomGrantTypes.Delegation,
        CustomGrantTypes.OtpAuthenticate,
        GrantType.ClientCredentials,
        GrantType.ResourceOwnerPassword,
        TotpConstants.GrantType.Totp
    };

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
