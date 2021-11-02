using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Data;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Configuration;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal class CampaignService : ICampaignService
    {
        public CampaignService(
            CampaingsDbContext dbContext,
            IOptions<GeneralSettings> generalSettings
        ) {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            GeneralSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
        }

        public CampaingsDbContext DbContext { get; }
        public GeneralSettings GeneralSettings { get; }

        public async Task<CampaignDetails> GetCampaignById(Guid campaignId) {
            var campaign = await DbContext
                .Campaigns
                .AsNoTracking()
                .Include(x => x.Attachment)
                .Select(campaign => new CampaignDetails {
                    ActionText = campaign.ActionText,
                    ActivePeriod = campaign.ActivePeriod,
                    Content = campaign.Content,
                    CreatedAt = campaign.CreatedAt,
                    Id = campaign.Id,
                    Image = campaign.Attachment != null ? new AttachmentLink {
                        Id = campaign.Attachment.Id,
                        FileGuid = campaign.Attachment.Guid,
                        ContentType = campaign.Attachment.ContentType,
                        Label = campaign.Attachment.Name,
                        Size = campaign.Attachment.ContentLength,
                        PermaLink = $"{GeneralSettings.Host.TrimEnd('/')}/api/campaigns/images/{(Base64Id)campaign.Attachment.Guid}.{Path.GetExtension(campaign.Attachment.Name).TrimStart('.')}"
                    } : null,
                    IsActive = campaign.IsActive,
                    IsGlobal = campaign.IsGlobal,
                    IsNotification = campaign.IsNotification,
                    Title = campaign.Title,
                    ActionUrl = campaign.ActionUrl
                })
                .SingleOrDefaultAsync(x => x.Id == campaignId);
            if (campaign == null) {
                return null;
            }
            var messagesCount = await DbContext.CampaignUsers.CountAsync(x => x.CampaignId == campaign.Id);
            campaign.MessagesCount = messagesCount;
            return campaign;
        }

        public Task<ResultSet<Campaign>> GetCampaigns(ListOptions options) => DbContext.Campaigns.AsNoTracking().Select(campaign => new Campaign {
            Id = campaign.Id,
            Title = campaign.Title,
            ActivePeriod = campaign.ActivePeriod,
            IsActive = campaign.IsActive,
            ActionText = campaign.ActionText,
            ActionUrl = campaign.ActionUrl,
            Content = campaign.Content,
            CreatedAt = campaign.CreatedAt,
            IsGlobal = campaign.IsGlobal,
            IsNotification = campaign.IsNotification
        })
        .ToResultSetAsync(options);
    }
}