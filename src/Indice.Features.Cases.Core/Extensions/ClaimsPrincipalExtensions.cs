using Indice.Features.Cases.Core;
using Indice.Security;

namespace System.Security.Claims;

/// <summary>
/// Cases extensions on <see cref="ClaimsPrincipal"/>
/// </summary>
public static class CasesClaimsPrincipalExtensions
{
    /// <summary>The oauth scope that protects the current resource (API)</summary>
    /// <remarks>Defaults to <strong>cases</strong></remarks>
    public static string Scope { get; set; } = CasesCoreConstants.DefaultScopeName;

    /// <summary>
    /// Return a system user to be used in scenarios with no HttpContext.
    /// </summary>
    public static ClaimsPrincipal SystemUser() {
        List<Claim> claims = [
            new (BasicClaimTypes.Scope, Scope),
            new (BasicClaimTypes.Subject, "Case API"),
            new (BasicClaimTypes.Email, "Case API"),
            new (BasicClaimTypes.GivenName, "Case API"),
            new (BasicClaimTypes.FamilyName, "Case API"),
            new ($"client_{BasicClaimTypes.System}", bool.TrueString) // Claim for "IsSystemClient"
        ];
        var identity = new ClaimsIdentity(claims, "System Authentication"); // By setting "Basic" we are making the identity "Authenticated" so we can user user.IsAuthenticated() property later in our code
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Get the <see cref="BasicClaimTypes.Subject"/> or the <see cref="BasicClaimTypes.ClientId"/> of a ClaimsPrincipal.
    /// </summary>
    /// <param name="user">The claims principal.</param>
    /// <returns></returns>
    public static string? FindSubjectIdOrClientId(this ClaimsPrincipal user) =>
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
}
