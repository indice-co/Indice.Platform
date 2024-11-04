#if NET7_0_OR_GREATER
#nullable enable

using System.Net.Mime;
using Indice.Features.Messages.AspNetCore.Endpoints;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides endpoints for managing contact-related operations, including retrieving, creating, and updating contacts.
/// </summary>
public static class ContactsApi
{
    /// <summary>Registers the endpoints for Contacts API.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    /// <returns></returns>
    public static void MapContacts(this IEndpointRouteBuilder routes) {
        var configuration = routes.ServiceProvider.GetRequiredService<IConfiguration>();
        var options = configuration.GetApiSettings();
        var group = routes.MapGroup("/api/contacts");
        group.WithTags("Contacts");

        var allowedScopes = new[] { options.ResourceName }.Where(x => x != null).ToArray();
        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
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

        group.MapGet("{contactId:guid}", ContactsHandlers.GetContactById)
             .WithName(nameof(ContactsHandlers.GetContactById))
             .WithSummary("Gets the contact with the specified id.")
             .WithDescription(ContactsHandlers.GET_CONTACT_BY_ID_DESCRIPTION);

        group.MapPost(string.Empty, ContactsHandlers.CreateContact)
             .WithName(nameof(ContactsHandlers.CreateContact))
             .WithSummary("Creates a new contact in the store.")
             .WithDescription(ContactsHandlers.CREATE_CONTACT_DESCRIPTION)
             .WithParameterValidation<CreateContactRequest>();

        group.MapPut("{contactId:guid}", ContactsHandlers.UpdateContact)
             .WithName(nameof(ContactsHandlers.UpdateContact))
             .WithSummary("Updates the specified contact in the store.")
             .WithDescription(ContactsHandlers.UPDATE_CONTACT_DESCRIPTION)
             .WithParameterValidation<UpdateContactRequest>();
    }
}

#nullable disable
#endif