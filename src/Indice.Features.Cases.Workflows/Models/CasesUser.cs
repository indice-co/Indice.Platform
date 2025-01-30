using System.Security.Claims;
using Indice.Security;

namespace Indice.Features.Cases.Workflows.Models;

/// <summary>Related to the user principal that performed an action.</summary>
public record CasesUser
{
    /// <summary>The Id of the user.</summary>
    public string? Id { get; set; }

    /// <summary>The name of the user.</summary>
    public string? Name { get; set; }

    /// <summary>The email of the user.</summary>
    public string? Email { get; set; }

    /// <summary>The timestamp the action happened.</summary>
    public DateTimeOffset? When { get; set; } = DateTimeOffset.Now;
    
    public static CasesUser Create(ClaimsPrincipal user, DateTimeOffset? now = null) {
        return Populate(null, user, now);
    }
    
    // todo: remove irrelevant
    private static CasesUser Populate(CasesUser? meta, ClaimsPrincipal user, DateTimeOffset? now = null) {
        meta ??= new CasesUser();

        /*
         * meta.Id logic:
         * When the ClaimsPrincipal has Subject, then there is an authorized user that access a case.
         * When the ClaimsPrincipal does not have Subject, we're creating a case through a proxy that has been  authorized via client-credentials.
         */

        var subject = user.FindFirstValue(BasicClaimTypes.Subject);
        meta.Id = string.IsNullOrWhiteSpace(subject)
            ? user.FindFirstValue(BasicClaimTypes.ClientId)
            : subject;
        meta.Email = string.IsNullOrWhiteSpace(subject)
            ? user.FindFirstValue(BasicClaimTypes.ClientId)
            : user.FindFirstValue(BasicClaimTypes.Email);
        meta.Name = string.IsNullOrWhiteSpace(subject)
            ? "system_user"
            : $"{user.FindFirstValue(BasicClaimTypes.GivenName)} {user.FindFirstValue(BasicClaimTypes.FamilyName)}".Trim();
        meta.When = now ?? DateTimeOffset.UtcNow;
        return meta;
    }
}