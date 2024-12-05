#nullable enable

using Indice.Features.Messages.AspNetCore.Endpoints;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides endpoints for managing contact-related operations, including retrieving, creating, and updating contacts.
/// </summary>
internal static class ContactsApi
{
    /// <summary>Registers the endpoints for Contacts API.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static RouteGroupBuilder MapContacts(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<MessageManagementOptions>>().Value;
        var group = routes.MapGroup(options.PathPrefix.TrimEnd('/') + "/contacts");
        if (!string.IsNullOrEmpty(options.GroupName)) {
            group.WithGroupName(options.GroupName);
        }
        group.WithTags("Contacts");
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

        group.MapGet(string.Empty, ContactsHandlers.GetContacts)
             .WithName(nameof(ContactsHandlers.GetContacts))
             .WithSummary("Gets the list of all contacts using the provided ListOptions.")
             .WithDescription(ContactsHandlers.GET_CONTACTS_DESCRIPTION);

        group.MapGet("{contactId}", ContactsHandlers.GetContactById)
             .WithName(nameof(ContactsHandlers.GetContactById))
             .WithSummary("Gets the contact with the specified id.")
             .WithDescription(ContactsHandlers.GET_CONTACT_BY_ID_DESCRIPTION);

        group.MapPost(string.Empty, ContactsHandlers.CreateContact)
             .WithName(nameof(ContactsHandlers.CreateContact))
             .WithSummary("Creates a new contact in the store.")
             .WithDescription(ContactsHandlers.CREATE_CONTACT_DESCRIPTION)
             .WithParameterValidation<CreateContactRequest>();

        group.MapPut("{contactId}", ContactsHandlers.UpdateContact)
             .WithName(nameof(ContactsHandlers.UpdateContact))
             .WithSummary("Updates the specified contact in the store.")
             .WithDescription(ContactsHandlers.UPDATE_CONTACT_DESCRIPTION)
             .WithParameterValidation<UpdateContactRequest>();

        group.MapPost("{recipientId}/refresh", ContactsHandlers.RefreshContact)
             .WithName(nameof(ContactsHandlers.RefreshContact))
             .WithSummary("Add or Updates a contact that matches the recepientId.")
             .WithDescription(ContactsHandlers.REFRESH_CONTACT_DESCRIPTION)
             .WithParameterValidation<CreateContactRequest>();
        return group;
    }
}

#nullable disable