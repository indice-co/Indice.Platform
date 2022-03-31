using Indice.AspNetCore.Features.Campaigns.Data;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.AspNetCore.Features.Campaigns.Models;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    public class MessageService : IMessageService
    {
        public MessageService(CampaignsDbContext dbContext) {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public CampaignsDbContext DbContext { get; }

        public async Task Create(CreateMessageRequest request) {
            var dbMessage = new DbMessage {
                Id = Guid.NewGuid(),
                RecipientId = request.RecipientId,
                Body = request.Body,
                Title = request.Title,
                CampaignId = request.CampaignId
            };
            DbContext.Messages.Add(dbMessage);
            await DbContext.SaveChangesAsync();
        }
    }
}
