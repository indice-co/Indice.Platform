using System.Security.Claims;
using Indice.Security;

namespace Indice.Features.Cases.Workflows.Extensions;

/// <summary>CasesClaimsPrincipalExtensions</summary>
public static class CasesClaimsPrincipalExtensions
{
    /// <summary>Claims of the implicitly created user.</summary>
    public static List<Claim> Claims = [
        new (BasicClaimTypes.Scope, "cases"),
        new (BasicClaimTypes.Subject, "Case API"),
        new (BasicClaimTypes.Email, "Case API"),
        new (BasicClaimTypes.GivenName, "Case API"),
        new (BasicClaimTypes.FamilyName, "Case API"),
        new ($"client_{BasicClaimTypes.System}", bool.TrueString) // Claim for "IsSystemClient"
    ];
    
    /// <summary>Return a system user to be used in scenarios with no HttpContext.</summary>
    public static ClaimsPrincipal SystemUser() {
        var identity = new ClaimsIdentity(Claims, "System Authentication"); // By setting "Basic" we are making the identity "Authenticated" so we can user user.IsAuthenticated() property later in our code
        return new ClaimsPrincipal(identity);
    }
}