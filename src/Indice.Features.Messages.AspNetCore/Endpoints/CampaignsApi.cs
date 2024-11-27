#nullable enable

using Indice.Features.Messages.AspNetCore.Endpoints;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Security;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides endpoints for managing campaign-related operations, including retrieving, creating, updating, publishing, 
/// and deleting campaigns, as well as handling attachments and statistics.
/// </summary>
internal static class CampaignsApi
{
    /// <summary>Registers the endpoints for Campaigns API.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static RouteGroupBuilder MapCampaigns(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<MessageManagementOptions>>().Value;
        var group = routes.MapGroup(options.PathPrefix.TrimEnd('/') + "/campaigns");
        if (!string.IsNullOrEmpty(options.GroupName)) {
            group.WithGroupName(options.GroupName);
        }
        group.WithTags("Campaigns");
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

        group.MapGet(string.Empty, CampaignsHandlers.GetCampaigns)
             .WithName(nameof(CampaignsHandlers.GetCampaigns))
             .WithSummary("Gets the list of all campaigns using the provided ListOptions.")
             .WithDescription(CampaignsHandlers.GET_CAMPAIGNS_DESCRIPTION);
             //.WithOpenApiEnum<MessageChannelKind>(nameof(CampaignListFilter.MessageChannelKind));

        group.MapGet("{campaignId}", CampaignsHandlers.GetCampaignById)
             .WithName(nameof(CampaignsHandlers.GetCampaignById))
             .WithSummary("Gets a campaign with the specified id.")
             .WithDescription(CampaignsHandlers.GET_CAMPAIGN_BY_ID_DESCRIPTION);

        group.MapPut("{campaignId}/publish", CampaignsHandlers.PublishCampaign)
             .WithName(nameof(CampaignsHandlers.PublishCampaign))
             .WithSummary("Publishes a campaign.")
             .WithDescription(CampaignsHandlers.PUBLISH_CAMPAIGN_DESCRIPTION);

        group.MapGet("{campaignId}/statistics", CampaignsHandlers.GetCampaignStatistics)
             .WithName(nameof(CampaignsHandlers.GetCampaignStatistics))
             .WithSummary("Gets the statistics for a specified campaign.")
             .WithDescription(CampaignsHandlers.GET_CAMPAIGN_STATISTICS_DESCRIPTION)
             .CacheOutput(policy => policy.SetVaryByRouteValue(["campaignId"])
                                          .SetAutoTag()
                                          .SetAuthorized()
                                          .Expire(TimeSpan.FromMinutes(5)))
                          .WithCacheTag("Campaign", ["campaignId"]);

        group.MapGet("{campaignId}/statistics/export", CampaignsHandlers.ExportCampaignStatistics)
             .WithName(nameof(CampaignsHandlers.ExportCampaignStatistics))
             .WithSummary("Gets the statistics for a specified campaign in the form of an Excel file.")
             .WithDescription(CampaignsHandlers.EXPORT_CAMPAIGN_STATISTICS_DESCRIPTION);

        group.MapPost(string.Empty, CampaignsHandlers.CreateCampaign)
             .WithName(nameof(CampaignsHandlers.CreateCampaign))
             .WithSummary("Creates a new campaign.")
             .WithDescription(CampaignsHandlers.CREATE_CAMPAIGN_DESCRIPTION)
             .WithParameterValidation<CreateCampaignRequest>();

        group.MapPut("{campaignId}", CampaignsHandlers.UpdateCampaign)
             .WithName(nameof(CampaignsHandlers.UpdateCampaign))
             .WithSummary("Updates an existing unpublished campaign.")
             .WithDescription(CampaignsHandlers.UPDATE_CAMPAIGN_DESCRIPTION)
             .WithParameterValidation<UpdateCampaignRequest>();

        group.MapDelete("{campaignId}", CampaignsHandlers.DeleteCampaign)
             .WithName(nameof(CampaignsHandlers.DeleteCampaign))
             .WithSummary("Permanently deletes a campaign.")
             .WithDescription(CampaignsHandlers.DELETE_CAMPAIGN_DESCRIPTION);

        group.MapPost("{campaignId}/attachment", CampaignsHandlers.UploadCampaignAttachment)
             .WithName(nameof(CampaignsHandlers.UploadCampaignAttachment))
             .WithSummary("Uploads an attachment for the specified campaign.")
             .WithDescription(CampaignsHandlers.UPLOAD_CAMPAIGN_ATTACHMENT_DESCRIPTION)
             .WithParameterValidation<UploadFileRequest>()
             .Accepts<UploadFileRequest>("multipart/form-data")
             .LimitUpload(options.FileUploadLimit);

        group.MapDelete("{campaignId}/attachments/{attachmentId}", CampaignsHandlers.DeleteCampaignAttachment)
             .WithName(nameof(CampaignsHandlers.DeleteCampaignAttachment))
             .WithSummary("Deletes the camapaign attachment")
             .WithDescription(CampaignsHandlers.DELETE_CAMPAIGN_ATTACHMENT_DESCRIPTION);

        group.MapGet("attachments/{fileGuid}.{format}", CampaignsHandlers.GetCampaignAttachment)
             .WithName(nameof(CampaignsHandlers.GetCampaignAttachment))
             .WithSummary("Gets the attachment associated with a campaign.")
             .WithDescription(CampaignsHandlers.GET_CAMPAIGN_ATTACHMENT_DESCRIPTION)
             .AllowAnonymous()
             .ExcludeFromDescription();

        return group;
    }
}

#nullable disable