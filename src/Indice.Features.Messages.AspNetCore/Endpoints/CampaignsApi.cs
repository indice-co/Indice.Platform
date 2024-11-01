#if NET7_0_OR_GREATER

using System.Net.Mime;
using Indice.AspNetCore.Filters;
using Indice.Configuration;
using Indice.Events;
using Indice.Features.Messages.AspNetCore.Endpoints;
using Indice.Features.Messages.AspNetCore.Extensions;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Manager;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Security;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;


public static class CampaignsApi
{
    /// <summary>Registers the endpoints for Campaigns API.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    /// <returns></returns>
    public static void MapCampaigns(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetService<IConfiguration>().GetApiSettings();
        var group = routes.MapGroup("/api/campaigns");
        group.WithTags("Campaigns");
        var allowedScopes = new[] { options.ResourceName }.Where(x => x != null).ToArray();

        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.WithHandledException<BusinessException>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("", CampaignsHandlers.GetCampaigns)
            .WithName(nameof(CampaignsHandlers.GetCampaigns))
            .WithSummary("Gets the list of all campaigns.")
            .WithDescription("test");

        group.MapGet("{campaignId:guid}", CampaignsHandlers.GetCampaignById)
            .Produces<CampaignDetails>(StatusCodes.Status200OK)
            .Produces<NotFound>(StatusCodes.Status404NotFound);

        group.MapPut("{campaignId:guid}/publish", CampaignsHandlers.PublishCampaign)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("{campaignId:guid}/statistics", CampaignsHandlers.GetCampaignStatistics)
            .Produces<CampaignStatistics>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapGet("{campaignId:guid}/statistics/export", CampaignsHandlers.ExportCampaignStatistics)
            .WithName("ExportCampaignStatistics");

        group.MapPost("", CampaignsHandlers.CreateCampaign)
            .Produces<Campaign>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("{campaignId:guid}", CampaignsHandlers.UpdateCampaign)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapDelete("{campaignId:guid}", CampaignsHandlers.DeleteCampaign)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("{campaignId}/attachment", CampaignsHandlers.UploadCampaignAttachment)
            .Produces<AttachmentLink>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Accepts<IFormFile>("multipart/form-data");

        group.MapDelete("{campaignId}/attachments/{attachmentId}", CampaignsHandlers.DeleteCampaignAttachment)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("attachments/{fileGuid}.{format}", CampaignsHandlers.GetCampaignAttachment)
            .AllowAnonymous()
            .Produces<IFormFile>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}

#endif