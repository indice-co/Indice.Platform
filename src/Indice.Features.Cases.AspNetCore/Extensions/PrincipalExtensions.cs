using System.Security.Claims;
using Indice.Security;

namespace Indice.Features.Cases.Extensions;

internal static class PrincipalExtensions
{
    public static string Scope { get; set; } = CasesApiConstants.Scope;

    /// <summary>
    /// Return a system user to be used in scenarios with no HttpContext.
    /// </summary>
    public static ClaimsPrincipal SystemUser() {
        var claims = new List<Claim> {
                new Claim(BasicClaimTypes.Scope, Scope),
                new Claim(BasicClaimTypes.Subject, "Case API"),
                new Claim(BasicClaimTypes.Email, "Case API"),
                new Claim(BasicClaimTypes.GivenName, "Case API"),
                new Claim(BasicClaimTypes.FamilyName, "Case API"),
                new Claim($"client_{BasicClaimTypes.System}", bool.TrueString) // Claim for "IsSystemClient"
            };
        var identity = new ClaimsIdentity(claims, "Basic"); // By setting "Basic" we are making the identity "Authenticated" so we can user user.IsAuthenticated() property later in our code
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Get the <see cref="BasicClaimTypes.Subject"/> or the <see cref="BasicClaimTypes.ClientId"/> of a ClaimsPrincipal.
    /// </summary>
    /// <param name="user">The claims principal.</param>
    /// <returns></returns>
    public static string FindSubjectIdOrClientId(this ClaimsPrincipal user) =>
        string.IsNullOrWhiteSpace(user.FindSubjectId()) ?
            user.FindFirstValue(BasicClaimTypes.ClientId) :
            user.FindSubjectId();

    /// <summary>Gets user's list of Role Claims</summary>
    /// <param name="user"></param>
    public static List<string> GetUserRoles(this ClaimsPrincipal user) =>
        user.Claims
            .Where(c => c.Type == BasicClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

    /// <summary>Gets user's group from Claims</summary>
    /// <param name="user"></param>
    /// <param name="groupClaimName">The name of the group claim</param>
    public static string GetUserGroup(this ClaimsPrincipal user, string groupClaimName) => user.FindFirstValue(groupClaimName);
}
