using System.Security.Claims;
using IdentityModel;
using Indice.Features.Messages.Core;

namespace Indice.Features.Messages.Tests.Security;
internal class DummyPrincipals
{
    public static ClaimsPrincipal IndiceUser => new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(JwtClaimTypes.GivenName, "Indice"),
            new Claim(JwtClaimTypes.FamilyName, "User"),
            new Claim(JwtClaimTypes.ClientId, "indice-client"),
            new Claim(JwtClaimTypes.Email, "company@indice.gr"),
            new Claim(JwtClaimTypes.Name, "indice"),
            new Claim(JwtClaimTypes.Subject, "6c9fa6dd-ede4-486b-bf91-6de18542da4a"),
            new Claim(JwtClaimTypes.Scope, MessagesApi.Scope),
            new Claim(JwtClaimTypes.Role, "Administrator"),
        }, "Dummy", JwtClaimTypes.Name, JwtClaimTypes.Role));

    public static ClaimsPrincipal TestClient => new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(JwtClaimTypes.ClientId, "Integration Tests Mock Client")
        }, "Dummy", JwtClaimTypes.Name, JwtClaimTypes.Role));

    public static ClaimsPrincipal SystemClient => new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(JwtClaimTypes.ClientId, "System Mock Client")
        }, "Dummy", JwtClaimTypes.Name, JwtClaimTypes.Role));
}
