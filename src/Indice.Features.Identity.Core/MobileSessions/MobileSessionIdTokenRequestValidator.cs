#if NET9_0_OR_GREATER
using Duende.IdentityModel;
using Duende.IdentityServer.Validation;
#else
using IdentityModel;
using IdentityServer4.Validation;
#endif

namespace Indice.Features.Identity.Core.MobileSessions;

/// <summary>
/// Emit the 'sid' claim for token endpoint (mobile) flows.
/// Failed logins never receive a session id.</summary>
public sealed class MobileSessionIdTokenRequestValidator : ICustomTokenRequestValidator
{
    /// <inheritdoc />
    public Task ValidateAsync(CustomTokenRequestValidationContext context) {
        var request = context.Result?.ValidatedRequest;
        if (request?.SessionId is { Length: > 0 }) {
            return Task.CompletedTask;
        }
        
        var sessionId = request?.GrantType switch {
            OidcConstants.GrantTypes.Password or
                CustomGrantTypes.DeviceAuthentication or
                CustomGrantTypes.Mfa or
                CustomGrantTypes.Delegation or
                TotpConstants.GrantType.Totp => request.Subject?.FindFirst(JwtClaimTypes.SessionId)?.Value,
#if !NET9_0_OR_GREATER 
            // IS4 does not restore the sid on refresh (Duende does), so read it from the stored access token
            OidcConstants.GrantTypes.RefreshToken => request.RefreshToken?.SessionId,
# endif
            _ => null
        };
        
        if (!string.IsNullOrWhiteSpace(sessionId)) {
            request!.SessionId = sessionId;
        }
        
        return Task.CompletedTask;
    }
}