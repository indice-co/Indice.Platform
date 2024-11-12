﻿#if NET7_0_OR_GREATER

using Indice.Features.Messages.AspNetCore.Endpoints;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Mime;

namespace Microsoft.AspNetCore.Routing;


/// <summary>
/// Provides endpoints for managing distribution lists, including retrieving, creating, updating, and deleting lists, as well as managing contacts within distribution lists.
/// </summary>
public static class DistributionListsApi
{
    /// <summary>Registers the endpoints for Contacts API.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static void MapDistributionLists(this IEndpointRouteBuilder routes) {
        //.RequireAuthorization(MessagesApi.AuthenticationScheme, MessagesApi.Policies.BeCampaignManager)
        var configuration = routes.ServiceProvider.GetRequiredService<IConfiguration>();
        var options = configuration.GetApiSettings();
        var group = routes.MapGroup("/api/campaign-management/distribution-lists");
        group.WithTags("DistributionLists");

        var allowedScopes = new[] { options.ResourceName }.Where(x => x != null).ToArray();
        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.MapGet("", DistributionListsHandlers.GetDistributionLists)
            .Produces<ResultSet<DistributionList>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json);

        group.MapGet("{distributionListId:guid}", DistributionListsHandlers.GetDistributionListById)
            .Produces<DistributionList>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("", DistributionListsHandlers.CreateDistributionList)
            .Produces<DistributionList>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapDelete("{distributionListId:guid}", DistributionListsHandlers.DeleteDistributionList)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("{distributionListId:guid}", DistributionListsHandlers.UpdateDistributionList)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("{distributionListId:guid}/contacts", DistributionListsHandlers.GetDistributionListContacts)
            .Produces<ResultSet<Contact>>(StatusCodes.Status200OK);

        group.MapPost("{distributionListId:guid}/contacts", DistributionListsHandlers.AddContactToDistributionList)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapDelete("{distributionListId:guid}/contacts/{contactId:guid}", DistributionListsHandlers.RemoveContactFromDistributionList)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);
    }
}

#endif