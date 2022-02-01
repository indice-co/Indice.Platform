using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Configuration;
using Indice.AspNetCore.Features.Campaigns.Data;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Configuration;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal class UserMessagesService : IUserMessagesService
    {
        public UserMessagesService(
            CampaignsDbContext dbContext,
            IOptions<CampaignsApiOptions> apiOptions,
            IOptions<GeneralSettings> generalSettings
        ) {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            ApiOptions = apiOptions?.Value ?? throw new ArgumentNullException(nameof(apiOptions));
            GeneralSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
        }

        public CampaignsDbContext DbContext { get; }
        public CampaignsApiOptions ApiOptions { get; }
        public GeneralSettings GeneralSettings { get; }

        public Task<UserMessage> GetMessageById(Guid messageId, string userCode) => GetUserMessagesQuery(userCode).SingleOrDefaultAsync(x => x.Id == messageId);

        public async Task<ResultSet<UserMessage, IEnumerable<CampaignType>>> GetUserMessages(string userCode, ListOptions<UserMessageFilter> options) {
            var userMessages = await GetUserMessagesQuery(userCode, options).ToResultSetAsync(options);
            var campaignTypes = await DbContext.CampaignTypes.Select(x => new CampaignType {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync();
            return new ResultSet<UserMessage, IEnumerable<CampaignType>> {
                Count = userMessages.Count,
                Items = userMessages.Items,
                Summary = campaignTypes
            };
        }

        public async Task MarkMessageAsDeleted(Guid messageId, string userCode) {
            var message = await DbContext.CampaignUsers.SingleOrDefaultAsync(x => x.CampaignId == messageId && x.UserCode == userCode);
            if (message != null) {
                message.IsDeleted = true;
                message.DeleteDate = DateTime.UtcNow;
            } else {
                DbContext.CampaignUsers.Add(new DbCampaignUser {
                    CampaignId = messageId,
                    DeleteDate = DateTime.UtcNow,
                    Id = Guid.NewGuid(),
                    IsDeleted = true,
                    UserCode = userCode
                });
            }
            await DbContext.SaveChangesAsync();
        }

        public async Task MarkMessageAsRead(Guid messageId, string userCode) {
            var message = await DbContext.CampaignUsers.SingleOrDefaultAsync(x => x.CampaignId == messageId && x.UserCode == userCode);
            if (message is not null) {
                message.IsRead = true;
                message.ReadDate = DateTime.UtcNow;
            } else {
                DbContext.CampaignUsers.Add(new DbCampaignUser {
                    CampaignId = messageId,
                    Id = Guid.NewGuid(),
                    IsRead = true,
                    ReadDate = DateTime.UtcNow,
                    UserCode = userCode
                });
            }
            await DbContext.SaveChangesAsync();
        }

        private IQueryable<UserMessage> GetUserMessagesQuery(string userCode, ListOptions<UserMessageFilter> options = null) {
            var query = DbContext.Campaigns
                .AsNoTracking()
                .Include(x => x.Attachment)
                .Include(x => x.Type)
                .SelectMany(
                    collectionSelector: campaign => DbContext.CampaignUsers.AsNoTracking().Where(x => x.CampaignId == campaign.Id && x.UserCode == userCode).DefaultIfEmpty(),
                    resultSelector: (campaign, message) => new { campaign, message }
                )
                .Where(x => x.campaign.Published
                        && (x.message == null || !x.message.IsDeleted)
                        && (x.campaign.IsGlobal || x.message == null || x.message.UserCode == userCode)
                        && (
                           options.Filter == null || (
                              (options.Filter.ShowExpired.Value || !x.campaign.ActivePeriod.To.HasValue || x.campaign.ActivePeriod.To.Value >= DateTime.UtcNow) &&
                              (options.Filter.TypeId.Length == 0 || x.campaign.Type == null || options.Filter.TypeId.Contains(x.campaign.Type.Id)) &&
                              (options.Filter.ActiveTo == null || (x.campaign.ActivePeriod.From ?? DateTimeOffset.MinValue) < options.Filter.ActiveTo.Value) &&
                              (options.Filter.ActiveFrom == null || (x.campaign.ActivePeriod.To ?? DateTimeOffset.MaxValue) > options.Filter.ActiveFrom.Value) &&
                              (options.Filter.IsRead == null || ((bool?)x.message.IsRead ?? false) == options.Filter.IsRead)
                           )
                        )
                )
                .Select(x => new UserMessage {
                    ActionText = x.campaign.ActionText,
                    ActionUrl = !string.IsNullOrEmpty(x.campaign.ActionUrl) ? $"{GeneralSettings.Host.TrimEnd('/')}/{ApiOptions.ApiPrefix}/campaigns/track/{(Base64Id)x.campaign.Id}" : null,
                    AttachmentUrl = x.campaign.Attachment != null ? $"{GeneralSettings.Host.TrimEnd('/')}/{ApiOptions.ApiPrefix}/campaigns/attachments/{(Base64Id)x.campaign.Attachment.Guid}.{Path.GetExtension(x.campaign.Attachment.Name).TrimStart('.')}" : null,
                    Content = x.campaign.Content,
                    CreatedAt = x.campaign.CreatedAt,
                    Id = x.campaign.Id,
                    IsRead = x.message != null && x.message.IsRead,
                    Title = x.campaign.Title,
                    ActivePeriod = x.campaign.ActivePeriod,
                    Type = x.campaign.Type != null ? new CampaignType {
                        Id = x.campaign.Type.Id,
                        Name = x.campaign.Type.Name
                    } : null
                });
            return query;
        }
    }
}
