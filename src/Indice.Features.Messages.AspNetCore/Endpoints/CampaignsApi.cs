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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;


public static class CampaignsApi
{
    public static void MapCampaigns(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetService<IConfiguration>().GetApiSettings();
        var group = routes.MapGroup("/api/campaigns");
        group.WithTags("Campaigns");
        var allowedScopes = new[] { options.ResourceName }.Where(x => x != null).ToArray();

        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("", CampaignsHandler.GetCampaigns)
            .Produces<ResultSet<Campaign>>(StatusCodes.Status200OK);

        group.MapGet("{campaignId:guid}", CampaignsHandler.GetCampaignById)
            .Produces<CampaignDetails>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPut("{campaignId:guid}/publish", CampaignsHandler.PublishCampaign)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("{campaignId:guid}/statistics", CampaignsHandler.GetCampaignStatistics)
            .Produces<CampaignStatistics>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapGet("{campaignId:guid}/statistics/export", CampaignsHandler.GetCampaignStatistics)
            .Produces<IFormFile>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("", CampaignsHandler.CreateCampaign)
            .Produces<Campaign>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("{campaignId:guid}", CampaignsHandler.UpdateCampaign)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapDelete("{campaignId:guid}", CampaignsHandler.DeleteCampaign)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("{campaignId}/attachment", CampaignsHandler.UploadCampaignAttachment)
            .Produces<AttachmentLink>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Accepts<IFormFile>("multipart/form-data");

        group.MapDelete("{campaignId}/attachments/{attachmentId}", CampaignsHandler.DeleteCampaignAttachment)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("attachments/{fileGuid}.{format}", CampaignsHandler.GetCampaignAttachment)
            .AllowAnonymous()
            .Produces<IFormFile>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}

#endif