using System.Collections.Generic;
using System.Security.Claims;
using IdentityModel;
using Indice.Security;

namespace Indice.Features.Cases.Extensions
{
    internal static class PrincipalExtensions
    {
        public static bool IsVendor(this ClaimsPrincipal user) =>
            user.IsAdmin()
            //|| (user.FindFirstValue(JwtClaimTypes.Email)?.ToLowerInvariant().EndsWith("vendoremail-from config") ?? false)
            ;

        public static ClaimsPrincipal SystemUser() {
            var claims = new List<Claim> {
                    new Claim(JwtClaimTypes.Scope, CasesApiConstants.Scope),
                    new Claim(JwtClaimTypes.Subject, "Case API"),
                    new Claim(JwtClaimTypes.Email, "Case API"),
                    new Claim(JwtClaimTypes.GivenName, "Case API"),
                    new Claim(JwtClaimTypes.FamilyName, "Case API"),
                    new Claim($"client_{BasicClaimTypes.System}", bool.TrueString) // Claim for "IsSystemClient"
                };
            var identity = new ClaimsIdentity(claims, "Basic"); // By setting "Basic" we are making the identity "Authenticated" so we can user user.IsAuthenticated() property later in our code
            return new ClaimsPrincipal(identity);
        }
    }
}
