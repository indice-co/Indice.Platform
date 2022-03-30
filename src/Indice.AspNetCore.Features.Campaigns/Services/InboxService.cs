using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Data;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.AspNetCore.Features.Campaigns.Exceptions;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Configuration;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal class InboxService : IInboxService
    {
        public InboxService(
            CampaignsDbContext dbContext,
            IOptions<CampaignInboxOptions> campaignInboxOptions,
            IOptions<GeneralSettings> generalSettings
        ) {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            CampaignInboxOptions = campaignInboxOptions?.Value ?? throw new ArgumentNullException(nameof(campaignInboxOptions));
            GeneralSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
        }

        public CampaignsDbContext DbContext { get; }
        public CampaignInboxOptions CampaignInboxOptions { get; }
        public GeneralSettings GeneralSettings { get; }

        public async Task<ResultSet<Message>> GetList(string userCode, ListOptions<MessagesFilter> options) {
            var userMessages = await GetUserInboxQuery(userCode, options).ToResultSetAsync(options);
            return userMessages;
        }

        public Task<Message> GetById(Guid id, string userCode) => GetUserInboxQuery(userCode).SingleOrDefaultAsync(x => x.Id == id);

        public async Task MarkAsDeleted(Guid id, string userCode) {
            var message = await DbContext.Messages.SingleOrDefaultAsync(x => x.CampaignId == id && x.RecipientId == userCode);
            if (message is not null) {
                if (message.IsDeleted) {
                    throw CampaignException.MessageAlreadyRead(id);
                }
                message.IsDeleted = true;
                message.DeleteDate = DateTime.UtcNow;
            } else {
                DbContext.Messages.Add(new DbMessage {
                    CampaignId = id,
                    DeleteDate = DateTime.UtcNow,
                    Id = Guid.NewGuid(),
                    IsDeleted = true,
                    RecipientId = userCode
                });
            }
            await DbContext.SaveChangesAsync();
        }

        public async Task MarkAsRead(Guid id, string userCode) {
            var message = await DbContext.Messages.SingleOrDefaultAsync(x => x.CampaignId == id && x.RecipientId == userCode);
            if (message is not null) {
                if (message.IsRead) {
                    throw CampaignException.MessageAlreadyRead(id);
                }
                message.IsRead = true;
                message.ReadDate = DateTime.UtcNow;
            } else {
                DbContext.Messages.Add(new DbMessage {
                    CampaignId = id,
                    Id = Guid.NewGuid(),
                    IsRead = true,
                    ReadDate = DateTime.UtcNow,
                    RecipientId = userCode
                });
            }
            await DbContext.SaveChangesAsync();
        }

        private IQueryable<Message> GetUserInboxQuery(string userCode, ListOptions<MessagesFilter> options = null) {
            var query = DbContext
                .Campaigns
                .AsNoTracking()
                .Include(x => x.Attachment)
                .Include(x => x.Type)
                .SelectMany(
                    collectionSelector: campaign => DbContext.Messages.AsNoTracking().Where(x => x.CampaignId == campaign.Id && x.RecipientId == userCode).DefaultIfEmpty(),
                    resultSelector: (campaign, message) => new { Campaign = campaign, Message = message }
                )
                .Where(x => x.Campaign.Published
                    && x.Campaign.DeliveryChannel.HasFlag(MessageDeliveryChannel.Inbox)
                    && (x.Message == null || !x.Message.IsDeleted)
                    && (x.Campaign.IsGlobal || (x.Message != null && x.Message.RecipientId == userCode))
                );
            if (options?.Filter is not null) {
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
            return query.Select(x => new Message {
                ActionText = x.Campaign.ActionText,
                ActionUrl = !string.IsNullOrEmpty(x.Campaign.ActionUrl) 
                    ? $"{CampaignInboxOptions.ApiPrefix}/messages/cta/{(Base64Id)x.Campaign.Id}"
                    : null,
                ActivePeriod = x.Campaign.ActivePeriod,
                AttachmentUrl = x.Campaign.Attachment != null 
                    ? $"{CampaignInboxOptions.ApiPrefix}/campaigns/attachments/{(Base64Id)x.Campaign.Attachment.Guid}.{Path.GetExtension(x.Campaign.Attachment.Name).TrimStart('.')}"
                    : null,
                Title = x.Message.Title,
                Content = x.Message.Body,
                CreatedAt = x.Campaign.CreatedAt,
                Id = x.Campaign.Id,
                IsRead = x.Message != null && x.Message.IsRead,
                Type = x.Campaign.Type != null ? new MessageType {
                    Id = x.Campaign.Type.Id,
                    Name = x.Campaign.Type.Name
                } : null
            });
        }
    }
}
