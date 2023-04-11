using System.Security.Claims;
using Indice.Security;

namespace Indice.Features.Cases.Extensions;

internal static class PrincipalExtensions
{
    public static ClaimsPrincipal SystemUser() {
        var claims = new List<Claim> {
                new Claim(BasicClaimTypes.Scope, CasesApiConstants.Scope),
                new Claim(BasicClaimTypes.Subject, "Case API"),
                new Claim(BasicClaimTypes.Email, "Case API"),
                new Claim(BasicClaimTypes.GivenName, "Case API"),
                new Claim(BasicClaimTypes.FamilyName, "Case API"),
                new Claim($"client_{BasicClaimTypes.System}", bool.TrueString) // Claim for "IsSystemClient"
            };
        var identity = new ClaimsIdentity(claims, "Basic"); // By setting "Basic" we are making the identity "Authenticated" so we can user user.IsAuthenticated() property later in our code
        return new ClaimsPrincipal(identity);
    }

    public static string FindSubjectIdOrClientId(this ClaimsPrincipal user) {
        var subjectId = user.FindSubjectId();
        return !string.IsNullOrWhiteSpace(subjectId) ? subjectId : user.FindFirstValue(BasicClaimTypes.ClientId);
    }
}
