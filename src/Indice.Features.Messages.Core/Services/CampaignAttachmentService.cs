﻿using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Exceptions;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.Core.Services;

/// <summary>An implementation of <see cref="ICampaignAttachmentService"/> for Entity Framework Core.</summary>
public class CampaignAttachmentService : ICampaignAttachmentService
{
    /// <summary>Creates a new instance of <see cref="CampaignService"/>.</summary>
    /// <param name="fileServiceFactory">File storage abstraction.</param>
    /// <param name="dbContext">The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for Campaigns API feature.</param>
    /// <param name="campaignManagementOptions">Options used to configure the Campaigns management API feature.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public CampaignAttachmentService(
        IFileServiceFactory fileServiceFactory,
        CampaignsDbContext dbContext,
        IOptions<MessageManagementOptions> campaignManagementOptions
    ) {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        FileService = fileServiceFactory.Create(KeyedServiceNames.FileServiceKey) ?? throw new ArgumentNullException(nameof(fileServiceFactory), $"Service {KeyedServiceNames.FileServiceKey} was not registered");
        CampaignManagementOptions = campaignManagementOptions?.Value ?? throw new ArgumentNullException(nameof(campaignManagementOptions));
    }

    private CampaignsDbContext DbContext { get; }
    private IFileService FileService { get; }
    private MessageManagementOptions CampaignManagementOptions { get; }

    /// <inheritdoc />
    public async Task<AttachmentLink> Create(FileAttachment fileAttachment) {
        var attachment = Mapper.ToDbAttachment(fileAttachment);
        using (var stream = fileAttachment.OpenReadStream()) {
            await FileService.SaveAsync($"campaigns/{attachment.Uri}", stream);
        }
        DbContext.Attachments.Add(attachment);
        await DbContext.SaveChangesAsync();
        return new AttachmentLink {
            ContentType = fileAttachment.ContentType,
            Id = attachment.Id,
            Label = fileAttachment.Name,
            PermaLink = $"{CampaignManagementOptions.PathPrefix.TrimEnd('/')}/campaigns/attachments/{(Base64Id)attachment.Guid}.{Path.GetExtension(attachment.Name)!.TrimStart('.')}",
            Size = fileAttachment.ContentLength
        };
    }

    /// <inheritdoc />
    public async Task Associate(Guid campaignId, Guid attachmentId) {
        var campaign = await DbContext.Campaigns.FindAsync(campaignId) ?? throw MessageExceptions.CampaignNotFound(campaignId);
        campaign.AttachmentId = attachmentId;
        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task Delete(Guid campaignId, Guid attachmentId) {
        var dbAttachment = await DbContext
            .Attachments
            .Where(x => x.Id == attachmentId)
            .SingleOrDefaultAsync()
            ?? throw MessageExceptions.AttachmentNotFound(attachmentId);
        var campaign = await DbContext.Campaigns.FindAsync(campaignId);
        if (campaign is null) {
            throw MessageExceptions.CampaignNotFound(campaignId);
        }
        campaign.AttachmentId = null;
        DbContext.Attachments.Remove(dbAttachment);
        await DbContext.SaveChangesAsync();

        var path = $"campaigns/{dbAttachment.Guid.ToString("N")[..2]}/{dbAttachment.Guid:N}.{dbAttachment.FileExtension?.TrimStart('.')}";
        await FileService.DeleteAsync(path);
    }

    /// <inheritdoc />
    public async Task<FileAttachment?> GetFile(Guid campaignId, Guid attachmentId) {
        var dbAttachment = await DbContext
            .Campaigns
            .Where(x => x.Id == campaignId)
            .Select(x => x.Attachment)
            .SingleOrDefaultAsync();
        if (dbAttachment is null) {
            return null;
        }
        var path = $"campaigns/{dbAttachment.Guid.ToString("N")[..2]}/{dbAttachment.Guid:N}.{dbAttachment.FileExtension?.TrimStart('.')}";
        var data = await FileService.GetAsync(path);
        if (data is null) {
            return null;
        }
        return new FileAttachment {
            ContentLength = dbAttachment.ContentLength,
            ContentType = dbAttachment.ContentType,
            Data = data,
            FileExtension = dbAttachment.FileExtension!,
            Guid = dbAttachment.Guid,
            Id = dbAttachment.Id,
            Name = dbAttachment.Name
        };
    }
}
