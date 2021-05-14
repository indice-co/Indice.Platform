using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Identity.Extensions
{
    /// <summary>
    /// Extension methods on <see cref="HttpContext"/>.
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Extracts information from the cookie created about the user that was authenticated using the idsrv.external scheme.
        /// </summary>
        /// <param name="httpContext">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
        public static async Task<(string PrincipalId, IEnumerable<Claim> PrincipalClaims, string ProviderName, AuthenticationProperties AuthenticationProperties)> GetExternalLoginInfo(this HttpContext httpContext) {
            // Read external identity from the temporary cookie.
            var result = await httpContext.AuthenticateAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);
            if (result?.Succeeded != true) {
                throw new Exception("External authentication error.");
            }
            // Retrieve claims of the external user.
            var externalUser = result.Principal;
            var claims = externalUser.Claims.ToList();
            // Try to determine the unique id of the external user (issued by the provider). The most common claim type for that are the sub claim and the NameIdentifier.
            // Depending on the external provider, some other claim type might be used.
            var userIdClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject);
            if (userIdClaim == null) {
                throw new Exception("Unknown user id.");
            }
            claims.Remove(userIdClaim);
            var provider = result.Properties.Items["scheme"];
            var userId = userIdClaim.Value;
            return (userId, claims, provider, result.Properties);
        }
    }
}
