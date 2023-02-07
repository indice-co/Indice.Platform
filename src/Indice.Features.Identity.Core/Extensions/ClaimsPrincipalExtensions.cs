using System.Security.Claims;
using Indice.Features.Identity.Core.Configuration;
using Indice.Features.Identity.Core.Models;

namespace Indice.Features.Identity.Core.Extensions
{
    /// <summary>Extension methods on <see cref="ClaimsPrincipal"/> type.</summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>Returns true if the principal has an identity with the specified cookie scheme.</summary>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/> instance.</param>
        /// <param name="authenticationScheme">The authentication scheme to check.</param>
        /// <returns>True if the user is logged in with specified identity and scheme.</returns>
        public static bool IsSignedInWithScheme<TUser>(this ClaimsPrincipal principal, string authenticationScheme) where TUser : User {
            if (principal == null) {
                throw new ArgumentNullException(nameof(principal));
            }
            return principal?.Identities != null && principal.Identities.Any(x => x.AuthenticationType == authenticationScheme); 
        }

        /// <summary>Returns true if the principal is partially signed in with the <see cref="ExtendedIdentityConstants.ExtendedValidationUserIdScheme"/>.</summary>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/> instance.</param>
        /// <returns>True if the user is logged in with specified identity and scheme.</returns>
        public static bool IsSignedInPartially<TUser>(this ClaimsPrincipal principal) where TUser : User =>
            principal.IsSignedInWithScheme<TUser>(ExtendedIdentityConstants.ExtendedValidationUserIdScheme);
    }
}
