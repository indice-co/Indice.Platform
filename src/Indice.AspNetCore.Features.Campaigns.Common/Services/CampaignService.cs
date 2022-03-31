﻿using Indice.AspNetCore.Features.Campaigns.Data;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.AspNetCore.Features.Campaigns.Exceptions;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Configuration;
using Indice.Services;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    public class CampaignService : ICampaignService
    {
        public CampaignService(
            CampaignsDbContext dbContext,
            IOptions<GeneralSettings> generalSettings,
            IOptions<CampaignManagementOptions> campaignManagementOptions,
            Func<string, IFileService> getFileService
        ) {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            CampaignManagementOptions = campaignManagementOptions?.Value ?? throw new ArgumentNullException(nameof(campaignManagementOptions));
            FileService = getFileService(KeyedServiceNames.FileServiceKey) ?? throw new ArgumentNullException(nameof(getFileService));
            GeneralSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
        }

        public CampaignsDbContext DbContext { get; }
        public CampaignManagementOptions CampaignManagementOptions { get; }
        public IFileService FileService { get; }
        public GeneralSettings GeneralSettings { get; }

        public Task<ResultSet<Campaign>> GetList(ListOptions<CampaignsFilter> options) {
            var query = DbContext
                .Campaigns
                .Include(x => x.Type)
                .Include(x => x.DistributionList)
                .AsNoTracking()
                .Select(Mapper.ProjectToCampaign);
            if (!string.IsNullOrEmpty(options.Search)) {
                var searchTerm = options.Search.Trim();
                query = query.Where(x => x.Title != null && x.Title.Contains(searchTerm));
            }
            if (options.Filter.DeliveryChannel.HasValue) {
                query = query.Where(x => x.DeliveryChannel.HasFlag(options.Filter.DeliveryChannel.Value));
            }
            if (options.Filter.Published.HasValue) {
                query = query.Where(x => x.Published == options.Filter.Published.Value);
            }
            return query.ToResultSetAsync(options);
        }

        public async Task<CampaignDetails> GetById(Guid id) {
            var campaign = await DbContext
                .Campaigns
                .AsNoTracking()
                .Include(x => x.Attachment)
                .Include(x => x.DistributionList)
                .Select(Mapper.ProjectToCampaignDetails)
                .SingleOrDefaultAsync(x => x.Id == id);
            if (campaign is null) {
                return default;
            }
            if (campaign.Attachment is not null) {
                campaign.Attachment.PermaLink = $"{CampaignManagementOptions.ApiPrefix}/{campaign.Attachment.PermaLink.TrimStart('/')}";
            }
            return campaign;
        }

        public async Task<Campaign> Create(CreateCampaignRequest request) {
            var dbCampaign = Mapper.ToDbCampaign(request);
            DbContext.Campaigns.Add(dbCampaign);
            await DbContext.SaveChangesAsync();
            return Mapper.ToCampaign(dbCampaign);
        }

        public async Task Update(Guid id, UpdateCampaignRequest request) {
            var campaign = await DbContext.Campaigns.FindAsync(id);
            if (campaign is null) {
                throw CampaignException.CampaignNotFound(id);
            }
            campaign.ActionText = request.ActionText;
            campaign.ActivePeriod = request.ActivePeriod;
            campaign.Content = request.Content;
            campaign.Title = request.Title;
            await DbContext.SaveChangesAsync();
        }

        public async Task Delete(Guid id) {
            var campaign = await DbContext.Campaigns.FindAsync(id);
            if (campaign is null) {
                throw CampaignException.CampaignNotFound(id);
            }
            DbContext.Remove(campaign);
            await DbContext.SaveChangesAsync();
        }

        public async Task<AttachmentLink> CreateAttachment(FileAttachment fileAttachment) {
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

        public async Task AssociateAttachment(Guid id, Guid attachmentId) {
            var campaign = await DbContext.Campaigns.FindAsync(id);
            if (campaign is null) {
                throw CampaignException.CampaignNotFound(id);
            }
            campaign.AttachmentId = attachmentId;
            await DbContext.SaveChangesAsync();
        }

        public async Task<CampaignStatistics> GetStatistics(Guid id) {
            var campaign = await DbContext.Campaigns.FindAsync(id);
            if (campaign is null) {
                return default;
            }
            var callToActionCount = await DbContext.Hits.AsNoTracking().CountAsync(x => x.CampaignId == id);
            var readCount = await DbContext.Messages.AsNoTracking().CountAsync(x => x.CampaignId == id && x.IsRead);
            var deletedCount = await DbContext.Messages.AsNoTracking().CountAsync(x => x.CampaignId == id && x.IsDeleted);
            int? notReadCount = null;
            if (!campaign.IsGlobal) {
                notReadCount = await DbContext.Messages.AsNoTracking().CountAsync(x => x.CampaignId == id && !x.IsRead);
            }
            return new CampaignStatistics {
                CallToActionCount = callToActionCount,
                DeletedCount = deletedCount,
                LastUpdated = DateTime.UtcNow,
                NotReadCount = notReadCount,
                ReadCount = readCount,
                Title = campaign.Title
            };
        }

        public async Task UpdateHit(Guid id) {
            DbContext.Hits.Add(new DbHit {
                CampaignId = id,
                TimeStamp = DateTimeOffset.UtcNow
            });
            await DbContext.SaveChangesAsync();
        }

        public async Task Publish(Guid id) {
            var campaign = await DbContext.Campaigns.FindAsync(id);
            if (campaign is null) {
                throw CampaignException.CampaignNotFound(id);
            }
            if (campaign.Published) {
                throw CampaignException.CampaignAlreadyPublished(id);
            }
            campaign.Published = true;
            await DbContext.SaveChangesAsync();
        }
    }
}
