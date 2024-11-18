#if NET7_0_OR_GREATER

using Indice.Features.Messages.AspNetCore.Endpoints;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides endpoints for managing distribution lists, including retrieving, creating, updating, and deleting lists, as well as managing contacts within distribution lists.
/// </summary>
public static class DistributionListsApi
{
    /// <summary>Registers the endpoints for Contacts API.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static RouteGroupBuilder MapDistributionLists(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<MessageManagementOptions>>().Value;
        var group = routes.MapGroup(options.ApiPrefix.TrimEnd('/') + "/distribution-lists"); 
        if (!string.IsNullOrEmpty(options.GroupName)) {
            group.WithGroupName(options.GroupName);
        }
        group.WithTags("DistributionLists");
        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).ToArray();

        group.RequireAuthorization(pb => pb.AddAuthenticationSchemes(MessagesApi.AuthenticationScheme)
                                           .RequireAuthenticatedUser()
                                           .RequireCampaignsManagement()
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.WithHandledException<BusinessException>()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("", DistributionListsHandlers.GetDistributionLists)
             .WithName(nameof(DistributionListsHandlers.GetDistributionLists))
             .WithSummary("Gets the list of available campaign types.");

        group.MapGet("{distributionListId}", DistributionListsHandlers.GetDistributionListById)
             .WithName(nameof(DistributionListsHandlers.GetDistributionListById))
             .WithSummary("Gets a distribution list by it's unique id.");

        group.MapPost("", DistributionListsHandlers.CreateDistributionList)
             .WithName(nameof(DistributionListsHandlers.CreateDistributionList))
             .WithSummary("Creates a new distribution list.")
             .WithParameterValidation<CreateDistributionListRequest>();

        group.MapDelete("{distributionListId}", DistributionListsHandlers.DeleteDistributionList)
             .WithName(nameof(DistributionListsHandlers.DeleteDistributionList))
             .WithSummary("Permanently deletes a distribution list.");

        group.MapPut("{distributionListId}", DistributionListsHandlers.UpdateDistributionList)
             .WithName(nameof(DistributionListsHandlers.UpdateDistributionList))
             .WithSummary("Updates an existing distribution list.")
             .WithParameterValidation<UpdateDistributionListRequest>();

        group.MapGet("{distributionListId}/contacts", DistributionListsHandlers.GetDistributionListContacts)
             .WithName(nameof(DistributionListsHandlers.GetDistributionListContacts))
             .WithSummary("Gets the contacts of a given distribution list.");

        group.MapPost("{distributionListId}/contacts", DistributionListsHandlers.AddContactToDistributionList)
             .WithName(nameof(DistributionListsHandlers.AddContactToDistributionList))
             .WithSummary("Adds a new or existing contact in the specified distribution list.")
             .WithParameterValidation<CreateDistributionListContactRequest>();

        group.MapDelete("{distributionListId}/contacts/{contactId}", DistributionListsHandlers.RemoveContactFromDistributionList)
             .WithName(nameof(DistributionListsHandlers.RemoveContactFromDistributionList))
             .WithSummary("Removes an existing contact from the specified distribution list.");

        return group;
    }
}

#endif