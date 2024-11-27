#nullable enable

using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Indice.Features.Messages.AspNetCore.Extensions;
using Indice.Features.Messages.Core.Manager;
using System.Net.Mime;
using Indice.Services;
using Indice.Features.Messages.Core;
using Indice.Extensions;
using Microsoft.Net.Http.Headers;
using Indice.Features.Messages.Core.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Messages.AspNetCore.Endpoints;

internal static class CampaignsHandlers
{
    public static async Task<Ok<ResultSet<Campaign>>> GetCampaigns(ICampaignService campaignService, [AsParameters] ListOptions options, [AsParameters] CampaignListFilter filter) {
        var campaigns = await campaignService.GetList(ListOptions.Create(options, filter));
        return TypedResults.Ok(campaigns);
    }

    public static async Task<Results<Ok<CampaignDetails>, NotFound>> GetCampaignById(ICampaignService campaignService, Guid campaignId) {
        var campaign = await campaignService.GetById(campaignId);
        if (campaign is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(campaign);
    }

    public static async Task<NoContent> PublishCampaign(ICampaignService campaignService, IEventDispatcher eventDispatcher, Guid campaignId) {
        var publishedCampaign = await campaignService.Publish(campaignId);

        await eventDispatcher.RaiseEventAsync(
            CampaignCreatedEvent.FromCampaign(publishedCampaign),
            builder => builder.WrapInEnvelope().WithQueueName(EventNames.CampaignCreated)
        );

        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok<CampaignStatistics>, NotFound>> GetCampaignStatistics(ICampaignService campaignService, Guid campaignId) {
        var statistics = await campaignService.GetStatistics(campaignId);
        if (statistics is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(statistics);
    }

    public static async Task<Results<Ok<CampaignStatistics>, NotFound>> ExportCampaignStatistics(ICampaignService campaignService, IFileServiceFactory fileServiceFactory, Guid campaignId) {
        var statistics = await campaignService.GetStatistics(campaignId);
        if (statistics == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(statistics);
    }

    public static async Task<Results<CreatedAtRoute<CreateCampaignResult>, ValidationProblem>> CreateCampaign(NotificationsManager notificationsManager, IConfiguration configuration, CreateCampaignRequest request) {
        if (request != null && string.IsNullOrWhiteSpace(request.MediaBaseHref)) {
            request.MediaBaseHref = configuration.GetHost();
        }
        var result = await notificationsManager.CreateCampaignInternal(request, validateRules: false);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(ValidationErrors.AddErrors("Campagin Validation", result.Errors));
        }
        return TypedResults.CreatedAtRoute(result, nameof(GetCampaignById), new { campaignId = result.CampaignId });
    }

    public static async Task<NoContent> UpdateCampaign(ICampaignService campaignService, Guid campaignId, UpdateCampaignRequest request) {
        await campaignService.Update(campaignId, request);
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> DeleteCampaign(ICampaignService campaignService, Guid campaignId) {
        await campaignService.Delete(campaignId);
        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok<AttachmentLink>, ValidationProblem>> UploadCampaignAttachment(ICampaignAttachmentService campaignAttachmentService, Guid campaignId, UploadFileRequest uploadFileRequest) {
        if (uploadFileRequest.File is null) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(uploadFileRequest.File), "The file is required"));
        }
        var attachment = new FileAttachment(() => uploadFileRequest.File.OpenReadStream())
                                            .PopulateFrom(uploadFileRequest.File);
        var attachmentLink = await campaignAttachmentService.Create(attachment);
        await campaignAttachmentService.Associate(campaignId, attachmentLink.Id);
        return TypedResults.Ok(attachmentLink);
    }

    public static async Task<NoContent> DeleteCampaignAttachment(ICampaignAttachmentService campaignAttachmentService, Guid campaignId, Guid attachmentId) {
        await campaignAttachmentService.Delete(campaignId, attachmentId);
        return TypedResults.NoContent();
    }

    public static async Task<Results<FileContentHttpResult, NotFound>> GetCampaignAttachment(IFileServiceFactory fileServiceFactory, Base64Id fileGuid, string format) {
        return await GetFile(fileServiceFactory, "campaigns", fileGuid, format);
    }

    public static async Task<Results<FileContentHttpResult, NotFound>> GetFile(IFileServiceFactory fileServiceFactory, string rootFolder, Guid fileGuid, string format) {
        var fileService = fileServiceFactory.Create(KeyedServiceNames.FileServiceKey)
                          ?? throw new ArgumentNullException(nameof(fileServiceFactory));

        if (format.StartsWith('.')) {
            format = format.TrimStart('.');
        }

        var path = $"{rootFolder}/{fileGuid.ToString("N")[..2]}/{fileGuid:N}.{format}";
        var properties = await fileService.GetPropertiesAsync(path);

        if (properties is null) {
            return TypedResults.NotFound();
        }

        var data = await fileService.GetAsync(path);
        var contentType = properties.ContentType;

        if (contentType == MediaTypeNames.Application.Octet && !string.IsNullOrEmpty(format)) {
            contentType = FileExtensions.GetMimeType($".{format}");
        }

        return TypedResults.File(data, contentType, lastModified: properties.LastModified, entityTag: new EntityTagHeaderValue(properties.ETag, true));
    }


    #region Descriptions
    public static readonly string GET_CAMPAIGNS_DESCRIPTION = @"
Retrieves the list of all campaigns based on the provided ListOptions.

Parameters:
- options: List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
";

    public static readonly string GET_CAMPAIGN_BY_ID_DESCRIPTION = @"
Retrieves a campaign with the specified ID.

Parameters:
- campaignId: The ID of the campaign to retrieve.
";

    public static readonly string PUBLISH_CAMPAIGN_DESCRIPTION = @"
Publishes a campaign.

Parameters:
- campaignId: The ID of the campaign to publish.
";

    public static readonly string GET_CAMPAIGN_STATISTICS_DESCRIPTION = @"
Retrieves the statistics for a specified campaign.

Parameters:
- campaignId: The ID of the campaign to retrieve statistics for.
";

    public static readonly string EXPORT_CAMPAIGN_STATISTICS_DESCRIPTION = @"
Retrieves the statistics for a specified campaign in the form of an Excel file.

Parameters:
- campaignId: The ID of the campaign to export statistics for.
";

    public static readonly string CREATE_CAMPAIGN_DESCRIPTION = @"
Creates a new campaign.

Parameters:
- request: Contains information about the campaign to be created.
";

    public static readonly string UPDATE_CAMPAIGN_DESCRIPTION = @"
Updates an existing unpublished campaign.

Parameters:
- campaignId: The ID of the campaign to update.
- request: Contains information about the campaign to update.
";

    public static readonly string DELETE_CAMPAIGN_DESCRIPTION = @"
Permanently deletes a campaign.

Parameters:
- campaignId: The ID of the campaign to delete.
";

    public static readonly string UPLOAD_CAMPAIGN_ATTACHMENT_DESCRIPTION = @"
Uploads an attachment for the specified campaign.

Parameters:
- campaignId: The ID of the campaign.
- file: Contains the stream of the attachment to be uploaded.
";

    public static readonly string DELETE_CAMPAIGN_ATTACHMENT_DESCRIPTION = @"
Deletes the campaign attachment.

Parameters:
- campaignId: The ID of the campaign.
- attachmentId: The ID of the attachment to be deleted.
";

    public static readonly string GET_CAMPAIGN_ATTACHMENT_DESCRIPTION = @"
Retrieves the attachment associated with a campaign.

Parameters:
- fileGuid: The ID of the attachment.
- format: The format of the uploaded attachment extension.
";

    #endregion
}

#nullable disable