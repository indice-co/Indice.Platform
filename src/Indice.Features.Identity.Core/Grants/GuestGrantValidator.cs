using System.Security.Claims;
using Indice.Security;
#if NET9_0_OR_GREATER
using Duende.IdentityModel;
#else
using IdentityModel;
#endif
#if NET9_0_OR_GREATER
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
#else
using IdentityServer4.Models;
using IdentityServer4.Validation;
#endif

namespace Indice.Features.Identity.Core.Grants;

/// <summary>A custom <see cref="IExtensionGrantValidator"/> that issues short-lived tokens for anonymous (guest) users.</summary>
/// <remarks>
/// The effective subject is echoed back through the token response custom field <c>sub</c>, so callers do not need to parse the access token.
/// Issued identities are discriminated by the <c>idp</c> claim with value <c>guest</c>.
/// Subclass and override <see cref="GetClaimsAsync"/> to validate additional request data and enrich the issued claims,
/// then register the subclass through <c>AddGuestGrantValidator&lt;TValidator&gt;()</c>.
/// </remarks>
public class GuestGrantValidator : IExtensionGrantValidator
{
    /// <summary>The identity provider value used to discriminate guest identities.</summary>
    public const string IdentityProviderName = "guest";

    /// <inheritdoc />
    public string GrantType => CustomGrantTypes.Guest;

    /// <inheritdoc />
    public virtual async Task ValidateAsync(ExtensionGrantValidationContext context) {
        string subject = Guid.NewGuid().ToString();
        IEnumerable<Claim> claims;
        try {
            claims = [.. GetProfileClaims(context), .. await GetClaimsAsync(context, subject) ?? []];
        } catch (InvalidOperationException exception) {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, exception.Message);
            return;
        }
        context.Result = new GrantValidationResult(
            subject: subject,
            authenticationMethod: CustomGrantTypes.Guest,
            claims: claims,
            identityProvider: IdentityProviderName,
            customResponse: new Dictionary<string, object> {
                [JwtClaimTypes.Subject] = subject
            }
        );
    }

    /// <summary>
    /// Parses the optional profile parameters supported out of the box (<c>given_name</c>, <c>family_name</c> and <c>email</c>)
    /// from the raw grant request and maps each one, when present, to the corresponding claim.
    /// </summary>
    /// <param name="context">The extension grant validation context.</param>
    protected static IEnumerable<Claim> GetProfileClaims(ExtensionGrantValidationContext context) {
        var raw = context.Request.Raw;
        var givenName = raw.Get(JwtClaimTypes.GivenName);
        if (!string.IsNullOrWhiteSpace(givenName)) {
            yield return new Claim(JwtClaimTypes.GivenName, givenName);
        }
        var familyName = raw.Get(JwtClaimTypes.FamilyName);
        if (!string.IsNullOrWhiteSpace(familyName)) {
            yield return new Claim(JwtClaimTypes.FamilyName, familyName);
        }
        var email = raw.Get(JwtClaimTypes.Email);
        if (!string.IsNullOrWhiteSpace(email)) {
            yield return new Claim(JwtClaimTypes.Email, email);
        }
        var phoneNumber = raw.Get(JwtClaimTypes.PhoneNumber);
        if (!string.IsNullOrWhiteSpace(phoneNumber)) {
            yield return new Claim(JwtClaimTypes.PhoneNumber, phoneNumber);
        }
        var device_id = raw.Get(BasicClaimTypes.DeviceId);
        if (!string.IsNullOrWhiteSpace(device_id)) {
            yield return new Claim(BasicClaimTypes.DeviceId, device_id);
        }
    }

    /// <summary>Builds the additional claims to include in the issued guest token. The base implementation returns an empty set.</summary>
    /// <remarks>Override to validate extra request data (available through <c>context.Request.Raw</c>) and/or include additional claims. Throw to reject the request with an <c>invalid_grant</c> error.</remarks>
    /// <param name="context">The extension grant validation context.</param>
    /// <param name="subject">The effective guest subject identifier.</param>
    protected virtual Task<IEnumerable<Claim>> GetClaimsAsync(ExtensionGrantValidationContext context, string subject) =>
        Task.FromResult(Enumerable.Empty<Claim>());
}
