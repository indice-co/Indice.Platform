using System;
using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Data;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    internal class MessageTypeService : IMessageTypeService
    {
        public MessageTypeService(CampaignsDbContext dbContext) {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public CampaignsDbContext DbContext { get; }

        public async Task<MessageType> Create(UpsertMessageTypeRequest request) {
            var messageType = new DbMessageType {
                Id = Guid.NewGuid(),
                Name = request.Name
            };
            DbContext.MessageTypes.Add(messageType);
            await DbContext.SaveChangesAsync();
            return new MessageType {
                Id = messageType.Id,
                Name = messageType.Name
            };
        }

        public async Task<bool> Delete(Guid campaignTypeId) {
            var messageType = await DbContext.MessageTypes.FindAsync(campaignTypeId);
            if (messageType is null) {
                return false;
            }
            DbContext.Remove(messageType);
            await DbContext.SaveChangesAsync();
            return true;
        }

        public async Task<MessageType> GetById(Guid campaignTypeId) {
            var messageType = await DbContext.MessageTypes.FindAsync(campaignTypeId);
            if (messageType is null) {
                return default;
            }
            return new MessageType {
                Id = messageType.Id,
                Name = messageType.Name
            };
        }

        public async Task<MessageType> GetByName(string name) {
            var messageType = await DbContext.MessageTypes.SingleOrDefaultAsync(x => x.Name == name);
            if (messageType is null) {
                return default;
            }
            return new MessageType {
                Id = messageType.Id,
                Name = messageType.Name
            };
        }

        public Task<ResultSet<MessageType>> GetList(ListOptions options) =>
            DbContext.MessageTypes
                     .AsNoTracking()
                     .Select(campaignType => new MessageType {
                         Id = campaignType.Id,
                         Name = campaignType.Name
                     })
                     .ToResultSetAsync(options);

        public async Task<bool> Update(Guid campaignTypeId, UpsertMessageTypeRequest request) {
            var messageType = await DbContext.MessageTypes.FindAsync(campaignTypeId);
            if (messageType is null) {
                return false;
            }
            messageType.Name = request.Name;
            await DbContext.SaveChangesAsync();
            return true;
        }
    }
}
