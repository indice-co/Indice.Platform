using System.Security.Claims;
using IdentityModel;
using Indice.Security;
namespace Indice.Features.Identity.Tests.Security;
internal class TestPrincipals
{
    public static ClaimsPrincipal UserWriter => new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(JwtClaimTypes.GivenName, "Indice"),
            new Claim(JwtClaimTypes.FamilyName, "User"),
            new Claim(JwtClaimTypes.ClientId, "indice-client"),
            new Claim(JwtClaimTypes.Email, "someone@indice.gr"),
            new Claim(JwtClaimTypes.Name, "indice"),
            new Claim(JwtClaimTypes.Role, BasicRoleNames.AdminUIUsersWriter),
            new Claim(JwtClaimTypes.Subject, "6c9fa6dd-ede4-486b-bf91-6de18542da4a"),
            new Claim(JwtClaimTypes.Scope, Indice.Features.Identity.Server.IdentityEndpoints.Scope),
            new Claim(JwtClaimTypes.Scope, Indice.Features.Identity.Server.IdentityEndpoints.SubScopes.Users),
            new Claim(JwtClaimTypes.Role, "Administrator"),
        ], "Dummy", JwtClaimTypes.Name, JwtClaimTypes.Role));

    public static ClaimsPrincipal TestClient => new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(JwtClaimTypes.ClientId, "Integration Tests Mock Client")
        ], "Dummy", JwtClaimTypes.Name, JwtClaimTypes.Role));

    public static ClaimsPrincipal SystemClient => new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(JwtClaimTypes.ClientId, "System Mock Client")
        ], "Dummy", JwtClaimTypes.Name, JwtClaimTypes.Role));
}
