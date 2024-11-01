#if NET7_0_OR_GREATER

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
    /// <summary>Gets the list of all campaigns using the provided <see cref="ListOptions"/>.</summary>
    /// <param name="campaignService">The service responsible for accessing and managing campaign data.</param>
    /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
    /// <param name="filter">Filtering criteria applied to refine the list of campaigns based on specific fields.</param>
    public static async Task<Ok<ResultSet<Campaign>>> GetCampaigns(ICampaignService campaignService, [AsParameters] ListOptions options, [AsParameters] CampaignListFilter filter) {
        var campaigns = await campaignService.GetList(ListOptions.Create(options, filter));
        return TypedResults.Ok(campaigns);
    }

    /// <summary>Gets a campaign with the specified id.</summary>
    /// <param name="campaignService">The service responsible for accessing and managing campaign data.</param>
    /// <param name="campaignId">The id of the campaign.</param>
    public static async Task<Results<Ok<CampaignDetails>, NotFound>> GetCampaignById(ICampaignService campaignService, Guid campaignId) {
        var campaign = await campaignService.GetById(campaignId);
        if (campaign is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(campaign);
    }

    /// <summary>Publishes a campaign.</summary>
    /// <param name="campaignService">The service responsible for accessing and managing campaign data.</param>
    /// <param name="eventDispatcher">Handles events raised when a campaign is published, allowing integration with other services.</param>
    /// <param name="campaignId">The id of the campaign.</param>
    public static async Task<NoContent> PublishCampaign(ICampaignService campaignService, IEventDispatcher eventDispatcher, Guid campaignId) {
        var publishedCampaign = await campaignService.Publish(campaignId);

        await eventDispatcher.RaiseEventAsync(
            CampaignCreatedEvent.FromCampaign(publishedCampaign),
            builder => builder.WrapInEnvelope().WithQueueName(EventNames.CampaignCreated)
        );

        return TypedResults.NoContent();
    }

    /// <summary>Gets the statistics for a specified campaign.</summary>
    /// <param name="campaignService">The service responsible for accessing and managing campaign data.</param>
    /// <param name="campaignId">The id of the campaign.</param>
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



    public static async Task<Created<Campaign>> CreateCampaign(NotificationsManager notificationsManager, IConfiguration configuration, CreateCampaignRequest request) {
        if (request != null && string.IsNullOrWhiteSpace(request.MediaBaseHref)) {
            request.MediaBaseHref = configuration.GetHost();
        }
        var result = await notificationsManager.CreateCampaignInternal(request, validateRules: false);
        return TypedResults.Created($"/campaigns/{result.CampaignId}", result.Campaign);
    }

    public static async Task<NoContent> UpdateCampaign(ICampaignService campaignService, Guid campaignId, UpdateCampaignRequest request) {
        await campaignService.Update(campaignId, request);
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> DeleteCampaign(ICampaignService campaignService, Guid campaignId) {
        await campaignService.Delete(campaignId);
        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok<AttachmentLink>, BadRequest<string>>> UploadCampaignAttachment(ICampaignAttachmentService campaignAttachmentService, Guid campaignId, IFormFile file) {
        if (file is null) {
            return TypedResults.BadRequest("File is empty.");
        }

        var attachment = new FileAttachment(() => file.OpenReadStream()).PopulateFrom(file);
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

    private static async Task<Results<FileContentHttpResult, NotFound>> GetFile(IFileServiceFactory fileServiceFactory, string rootFolder, Guid fileGuid, string format) {
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

}

#endif