using System.Security.Claims;
using Indice.Features.Cases.Workflows.Integrations;
using Indice.Security;

namespace Indice.Features.Cases.Workflows.Models;

/// <summary>
/// Represents a user/contact/client that is acting on the workflow. <see cref="CasesWorkflowConstants.WorkflowVariables.Actor"/> for supported types.
/// All exposed endpoints should receive Actor in the request body and update the corresponding Elsa variable
/// Currently this is used as information metadata for activities that need to know the last actor in the running context.
/// Clients may resolve additional user information with their identity provider/>
/// </summary>
public sealed record Actor
{
    /// <summary>The Id of the user.</summary>
    public string? Id { get; set; } = null!;
    
    /// <summary>Can be the customer id or something related to an external system correlation id</summary>
    public string? Reference { get; set; }
    
    /// <summary>The group id claim value.</summary> 
    public string? GroupId { get; set; }

    /// <summary>The name of the user.</summary>
    public string? Name { get; set; }

    /// <summary>The email of the user.</summary>
    public string? Email { get; set; }
    
    /// <summary>The current culture of the user.</summary>
    public string? CurrentCulture { get; set; }
    
    /// <summary>Creates an Actor given a <see cref="ClaimsPrincipal"/>.</summary>
    public static Actor Create(ClaimsPrincipal user, DateTimeOffset? now = null) {
        return Populate(null, user, now);
    }
    
    // todo: remove irrelevant. SystemUser() HAS subject
    private static Actor Populate(Actor? meta, ClaimsPrincipal user, DateTimeOffset? now = null) {
        meta ??= new Actor();

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
        return meta;
    }
}