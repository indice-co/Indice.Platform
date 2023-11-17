using System.Security.Claims;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Extensions;

/// <summary>Extension methods on <see cref="IEnumerable{Claim}"/> type.</summary>
public static class EnumerableExtensions
{
    /// <summary>Indicates whether MFA has been passed.</summary>
    /// <param name="claims">The list of claims.</param>
    public static bool MfaPassed(this IEnumerable<Claim> claims) => 
        !string.IsNullOrWhiteSpace(claims.FirstOrDefault(x => x.Type == CustomGrantTypes.Mfa)?.Value);
}
