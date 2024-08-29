/* https://identityserver4.readthedocs.io/en/latest/topics/extension_grants.html */

using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;

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
        var subject = result.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject)?.Value;
        context.Result = new GrantValidationResult(subject, GrantType);
    }
}
