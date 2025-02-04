using System.Security.Claims;
using Indice.Security;

namespace Indice.Features.Cases.Workflows.Extensions;

/// <summary>CasesClaimsPrincipalExtensions</summary>
public static class CasesClaimsPrincipalExtensions
{
    // todo: should we have elsa api system user?
    /// <summary>Return a system user to be used in scenarios with no HttpContext.</summary>
    public static ClaimsPrincipal SystemUser() {
        List<Claim> claims = [
            new (BasicClaimTypes.Scope, "cases"),
            new (BasicClaimTypes.Subject, "Case API"),
            new (BasicClaimTypes.Email, "Case API"),
            new (BasicClaimTypes.GivenName, "Case API"),
            new (BasicClaimTypes.FamilyName, "Case API"),
            new ($"client_{BasicClaimTypes.System}", bool.TrueString) // Claim for "IsSystemClient"
        ];
        var identity = new ClaimsIdentity(claims, "System Authentication"); // By setting "Basic" we are making the identity "Authenticated" so we can user user.IsAuthenticated() property later in our code
        return new ClaimsPrincipal(identity);
    }
}