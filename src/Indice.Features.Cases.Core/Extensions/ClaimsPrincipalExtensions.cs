using System.Globalization;
using System.Numerics;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Models;
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

    /// <summary>
    /// Get <see cref="WorkflowActor"/> from a ClaimsPrincipal.
    /// </summary>
    /// <param name="user">The claims principal.</param>
    /// <param name="options">Case options settings</param>
    /// <returns></returns>
    /// <summary>Creates a http <see cref="WorkflowActor"/> model from the current user.</summary>
    public static WorkflowActor UserToActor(this ClaimsPrincipal user, CasesOptions options) {
        var subject = user.FindFirstValue(BasicClaimTypes.Subject);
        return new WorkflowActor {
            Id = string.IsNullOrWhiteSpace(subject) ? user.FindFirstValue(BasicClaimTypes.ClientId) : subject,
            Reference = user.FindFirstValue(options.ReferenceIdClaimType),
            GroupId = user.FindFirstValue(options.GroupIdClaimType),
            Email = string.IsNullOrWhiteSpace(subject) ? user.FindFirstValue(BasicClaimTypes.ClientId) : user.FindFirstValue(BasicClaimTypes.Email),
            Name = string.IsNullOrWhiteSpace(subject) ? CasesCoreConstants.SystemUserName : $"{user.FindFirstValue(BasicClaimTypes.GivenName)} {user.FindFirstValue(BasicClaimTypes.FamilyName)}".Trim(),
            CurrentCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            IsAdmin = user.IsAdmin() || user.HasRoleClaim(BasicRoleNames.CasesAdministrator),
            IsSystemClient = user.IsSystemClient(),
            Roles = user.GetUserRoles(),
            Tin = user.FindFirstValue(options.TinClaimType)
        };
    }


}
