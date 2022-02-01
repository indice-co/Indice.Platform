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

        public async Task<ResultSet<UserMessage>> GetUserMessages(string userCode, ListOptions<UserMessageFilter> options) {
            var userMessages = await GetUserMessagesQuery(userCode, options).ToResultSetAsync(options);
            return new ResultSet<UserMessage> {
                Count = userMessages.Count,
                Items = userMessages.Items
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
            var query = DbContext
                .Campaigns
                .AsNoTracking()
                .Include(x => x.Attachment)
                .Include(x => x.Type)
                .SelectMany(
                    collectionSelector: campaign => DbContext.CampaignUsers.AsNoTracking().Where(x => x.CampaignId == campaign.Id && x.UserCode == userCode).DefaultIfEmpty(),
                    resultSelector: (campaign, message) => new { Campaign = campaign, Message = message }
                );
            if (options?.Filter is not null) {
                query = query.Where(
                    x => x.Campaign.Published 
                     && (x.Message == null || !x.Message.IsDeleted) 
                     && (x.Campaign.IsGlobal || x.Message == null || x.Message.UserCode == userCode)
                );
                if (options.Filter.ShowExpired.HasValue) {
                    query = query.Where(x => !x.Campaign.ActivePeriod.To.HasValue || x.Campaign.ActivePeriod.To.Value >= DateTime.UtcNow);
                }
                if (options.Filter.TypeId.Length > 0) {
                    query = query.Where(x => x.Campaign.Type == null || options.Filter.TypeId.Contains(x.Campaign.Type.Id));
                }
                if (options.Filter.ActiveFrom.HasValue) {
                    query = query.Where(x => (x.Campaign.ActivePeriod.To ?? DateTimeOffset.MaxValue) > options.Filter.ActiveFrom.Value);
                }
                if (options.Filter.ActiveTo.HasValue) {
                    query = query.Where(x => (x.Campaign.ActivePeriod.From ?? DateTimeOffset.MinValue) < options.Filter.ActiveTo.Value);
                }
                if (options.Filter.IsRead.HasValue) {
                    query = query.Where(x => ((bool?)x.Message.IsRead ?? false) == options.Filter.IsRead);
                }
            }
            return query.Select(x => new UserMessage {
                ActionText = x.Campaign.ActionText,
                ActionUrl = !string.IsNullOrEmpty(x.Campaign.ActionUrl) ? $"{GeneralSettings.Host.TrimEnd('/')}/{ApiOptions.ApiPrefix}/campaigns/track/{(Base64Id)x.Campaign.Id}" : null,
                ActivePeriod = x.Campaign.ActivePeriod,
                AttachmentUrl = x.Campaign.Attachment != null ? $"{GeneralSettings.Host.TrimEnd('/')}/{ApiOptions.ApiPrefix}/campaigns/attachments/{(Base64Id)x.Campaign.Attachment.Guid}.{Path.GetExtension(x.Campaign.Attachment.Name).TrimStart('.')}" : null,
                Content = x.Campaign.Content,
                CreatedAt = x.Campaign.CreatedAt,
                Id = x.Campaign.Id,
                IsRead = x.Message != null && x.Message.IsRead,
                Title = x.Campaign.Title,
                Type = x.Campaign.Type != null ? new CampaignType {
                    Id = x.Campaign.Type.Id,
                    Name = x.Campaign.Type.Name
                } : null
            });
        }
    }
}
