using Indice.AspNetCore.Features.Campaigns.Data;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.AspNetCore.Features.Campaigns.Models;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    /// <summary>
    /// An implementation of <see cref="IMessageService"/> for Entity Framework Core.
    /// </summary>
    public class MessageService : IMessageService
    {
        /// <summary>
        /// Creates a new instance of <see cref="MessageService"/>.
        /// </summary>
        /// <param name="dbContext">The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for Campaigns API feature.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public MessageService(CampaignsDbContext dbContext) {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        private CampaignsDbContext DbContext { get; }

        /// <inheritdoc />
        public async Task Create(CreateMessageRequest request) {
            var dbMessage = new DbMessage {
                Body = request.Body,
                CampaignId = request.CampaignId,
                Id = Guid.NewGuid(),
                RecipientId = request.RecipientId,
                Title = request.Title
            };
            DbContext.Messages.Add(dbMessage);
            await DbContext.SaveChangesAsync();
        }
    }
}
