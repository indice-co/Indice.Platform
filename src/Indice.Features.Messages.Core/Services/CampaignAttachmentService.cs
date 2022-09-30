using System.Net.Http.Headers;
using System.Net.Mime;
using Indice.Extensions;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Exceptions;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.Core.Services
{
    /// <summary>An implementation of <see cref="ICampaignAttachmentService"/> for Entity Framework Core.</summary>
    public class CampaignAttachmentService : ICampaignAttachmentService
    {
        /// <summary>Creates a new instance of <see cref="CampaignService"/>.</summary>
        /// <param name="getFileService">File storage abstraction.</param>
        /// <param name="dbContext">The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for Campaigns API feature.</param>
        /// <param name="campaignManagementOptions">Options used to configure the Campaigns management API feature.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public CampaignAttachmentService(
            Func<string, IFileService> getFileService,
            CampaignsDbContext dbContext,
            IOptions<MessageManagementOptions> campaignManagementOptions
        ) {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            FileService = getFileService(KeyedServiceNames.FileServiceKey) ?? throw new ArgumentNullException(nameof(getFileService));
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
                PermaLink = $"{CampaignManagementOptions.ApiPrefix}/campaigns/attachments/{(Base64Id)attachment.Guid}.{Path.GetExtension(attachment.Name).TrimStart('.')}",
                Size = fileAttachment.ContentLength
            };
        }

        /// <inheritdoc />
        public async Task Associate(Guid campaignId, Guid attachmentId) {
            var campaign = await DbContext.Campaigns.FindAsync(campaignId);
            if (campaign is null) {
                throw MessageExceptions.CampaignNotFound(campaignId);
            }
            campaign.AttachmentId = attachmentId;
            await DbContext.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<FileAttachment> GetFile(Guid campaignId, Guid attachmentId) {
            var dbAttachment = await DbContext.Campaigns.Where(x => x.Id == campaignId).Select(x => x.Attachment).SingleOrDefaultAsync();
            if (dbAttachment is null) {
                return null;
            }
            var path = $"campaigns/{dbAttachment.Uri}";
            var data = await FileService.GetAsync(path);
            if (data is null) {
                return null;
            }
            return new FileAttachment { 
                ContentType = dbAttachment.ContentType,
                Data = data,
                ContentLength = dbAttachment.ContentLength,
                Id = dbAttachment.Id,
                FileExtension = dbAttachment.FileExtension,
                Guid = dbAttachment.Guid,
                Name = dbAttachment.Name
            };
        }
    }
}
