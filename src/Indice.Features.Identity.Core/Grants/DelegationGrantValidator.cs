using Duende.IdentityModel;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using System.Security.Claims;
using Indice.Security;

namespace Indice.Features.Identity.Core.Grants;

/// <summary>A custom <see cref="IExtensionGrantValidator"/> that implements token delegation.</summary>
public class DelegationGrantValidator : IExtensionGrantValidator
{
    private readonly ITokenValidator _validator;

    /// <summary>Creates a new instance of <see cref="DelegationGrantValidator"/>.</summary>
    /// <param name="validator">Validates an access token.</param>
    public DelegationGrantValidator(ITokenValidator validator) {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <inheritdoc />
    public string GrantType => CustomGrantTypes.Delegation;

    /// <inheritdoc />
    public async Task ValidateAsync(ExtensionGrantValidationContext context) {
        var userToken = context.Request.Raw.Get("token");
        if (string.IsNullOrEmpty(userToken)) {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant);
            return;
        }
        var result = await _validator.ValidateAccessTokenAsync(userToken);
        if (result.IsError) {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant);
            return;
        }
        var subject = result.Claims?.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject)?.Value;
        context.Request.SessionId = result.Claims?.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId)?.Value;
        var claims = new List<Claim>();
        var deviceId = result.Claims?.FirstOrDefault(x => x.Type == BasicClaimTypes.DeviceId)?.Value;
        if (!string.IsNullOrWhiteSpace(deviceId)) {
            claims.Add(new Claim(BasicClaimTypes.DeviceId, deviceId));
        }

        context.Result = new GrantValidationResult(subject, GrantType, claims);
    }
}
