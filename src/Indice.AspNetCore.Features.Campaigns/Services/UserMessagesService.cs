using System;
using System.Linq;
using System.Threading.Tasks;
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
            CampaingsDbContext dbContext,
            IOptions<GeneralSettings> generalSettings
        ) {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            GeneralSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
        }

        public CampaingsDbContext DbContext { get; }
        public GeneralSettings GeneralSettings { get; }

        public Task<UserMessage> GetMessageById(Guid messageId, string userCode) => GetUserMessagesQuery(userCode).SingleOrDefaultAsync(x => x.Id == messageId);

        public Task<int> GetNumberOfUnreadMessages(string userCode) => GetUserMessagesQuery(userCode).Where(x => !x.IsRead).CountAsync();

        public Task<ResultSet<UserMessage>> GetUserMessages(string userCode, ListOptions options) => GetUserMessagesQuery(userCode).ToResultSetAsync(options);

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

        private IQueryable<UserMessage> GetUserMessagesQuery(string userCode) =>
            from campaign in DbContext.Campaigns.Include(x => x.Attachment).AsNoTracking()
            from message in DbContext.CampaignUsers.AsNoTracking().Where(x => x.CampaignId == campaign.Id && x.UserCode == userCode).DefaultIfEmpty()
            where campaign.IsActive
               && (message == null || !message.IsDeleted)
               && (campaign.IsGlobal || message == null || message.UserCode == userCode)
               && (!campaign.ActivePeriod.From.HasValue || campaign.ActivePeriod.From.Value <= DateTime.UtcNow)
               && (!campaign.ActivePeriod.To.HasValue || campaign.ActivePeriod.To.Value >= DateTime.UtcNow)
            select new UserMessage {
                ActionText = campaign.ActionText,
                ActionUrl = !string.IsNullOrEmpty(campaign.ActionUrl) ? $"{GeneralSettings.Host.TrimEnd('/')}/api/track/{(Base64Id)campaign.Id}" : null,
                AttachmentUrl = campaign.Attachment != null ? $"{GeneralSettings.Host.TrimEnd('/')}/api/campaigns/attachments/{(Base64Id)campaign.Attachment.Guid}.{System.IO.Path.GetExtension(campaign.Attachment.Name).TrimStart('.')}" : null,
                Content = campaign.Content,
                CreatedAt = campaign.CreatedAt,
                Id = campaign.Id,
                IsRead = message != null && message.IsRead,
                Title = campaign.Title
            };
    }
}
