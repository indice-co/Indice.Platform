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

        public Task<int> GetNumberOfUnreadMessages(string userCode) => GetUserMessagesQuery(userCode).Where(x => !x.IsRead).CountAsync();

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
            var query = from campaign in DbContext.Campaigns.AsNoTracking().Include(x => x.Attachment)
                        from message in DbContext.CampaignUsers.AsNoTracking().Where(x => x.CampaignId == campaign.Id && x.UserCode == userCode).DefaultIfEmpty()
                        where campaign.Published
                           && (message == null || !message.IsDeleted)
                           && (campaign.IsGlobal || message == null || message.UserCode == userCode)
                           && (!campaign.ActivePeriod.From.HasValue || campaign.ActivePeriod.From.Value <= DateTime.UtcNow)
                           && options.Filter.ShowExpired.Value || (!campaign.ActivePeriod.To.HasValue || campaign.ActivePeriod.To.Value >= DateTime.UtcNow)
                           && (options.Filter.TypeId == null || (campaign.Type != null && campaign.Type.Id == options.Filter.TypeId))
                           && (!options.Filter.ActiveFrom.HasValue || (campaign.ActivePeriod.From.HasValue && campaign.ActivePeriod.From.Value >= options.Filter.ActiveFrom.Value) 
                           ||  !options.Filter.ActiveTo.HasValue || (campaign.ActivePeriod.To.HasValue && campaign.ActivePeriod.To.Value <= options.Filter.ActiveTo.Value))
                        select new UserMessage {
                            ActionText = campaign.ActionText,
                            ActionUrl = !string.IsNullOrEmpty(campaign.ActionUrl) ? $"{GeneralSettings.Host.TrimEnd('/')}/{ApiOptions.ApiPrefix}/campaigns/track/{(Base64Id)campaign.Id}" : null,
                            AttachmentUrl = campaign.Attachment != null ? $"{GeneralSettings.Host.TrimEnd('/')}/{ApiOptions.ApiPrefix}/campaigns/attachments/{(Base64Id)campaign.Attachment.Guid}.{Path.GetExtension(campaign.Attachment.Name).TrimStart('.')}" : null,
                            Content = campaign.Content,
                            CreatedAt = campaign.CreatedAt,
                            Id = campaign.Id,
                            IsRead = message != null && message.IsRead,
                            Title = campaign.Title,
                            ActivePeriod = campaign.ActivePeriod,
                            Type = campaign.Type != null ? new CampaignType {
                                Id = campaign.Type.Id,
                                Name = campaign.Type.Name
                            } : null
                        };
            return query;
        }
    }
}
