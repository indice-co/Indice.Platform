#if NET7_0_OR_GREATER

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


public static class ContactsApi
{
    public static void MapContacts(this IEndpointRouteBuilder routes) {
        var configuration = routes.ServiceProvider.GetRequiredService<IConfiguration>();
        var options = configuration.GetApiSettings();
        var group = routes.MapGroup("/api/contacts");
        group.WithTags("Contacts");

        var allowedScopes = new[] { options.ResourceName }.Where(x => x != null).ToArray();
        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("", ContactsHandlers.GetContacts)
            .Produces<ResultSet<Contact>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json);

        group.MapGet("{contactId:guid}", ContactsHandlers.GetContactById)
            .Produces<Contact>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", ContactsHandlers.CreateContact)
            .Produces<MessageType>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("{contactId:guid}", ContactsHandlers.UpdateContact)
            .Produces(StatusCodes.Status204NoContent);
    }
}

#endif