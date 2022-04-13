using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Exceptions;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;
using Indice.Types;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.Core.Services
{
    /// <summary>
    /// An implementation of <see cref="ICampaignAttachmentService"/> for Entity Framework Core.
    /// </summary>
    public class CampaignAttachmentService : ICampaignAttachmentService
    {
        /// <summary>
        /// Creates a new instance of <see cref="CampaignService"/>.
        /// </summary>
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
        public async Task Associate(Guid id, Guid attachmentId) {
            var campaign = await DbContext.Campaigns.FindAsync(id);
            if (campaign is null) {
                throw CampaignException.CampaignNotFound(id);
            }
            campaign.AttachmentId = attachmentId;
            await DbContext.SaveChangesAsync();
        }
    }
}
