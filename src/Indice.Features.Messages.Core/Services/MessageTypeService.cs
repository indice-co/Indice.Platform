using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Exceptions;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Messages.Core.Services
{
    /// <summary>
    /// An implementation of <see cref="IMessageTypeService"/> for Entity Framework Core.
    /// </summary>
    public class MessageTypeService : IMessageTypeService
    {
        /// <summary>
        /// Creates a new instance of <see cref="MessageTypeService"/>.
        /// </summary>
        /// <param name="dbContext">The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for Campaigns API feature.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public MessageTypeService(CampaignsDbContext dbContext) {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        private CampaignsDbContext DbContext { get; }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public async Task Delete(Guid id) {
            var messageType = await DbContext.MessageTypes.FindAsync(id);
            if (messageType is null) {
                throw MessageException.MessageTypeNotFound(id);
            }
            DbContext.Remove(messageType);
            await DbContext.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<MessageType> GetById(Guid id) {
            var messageType = await DbContext.MessageTypes.FindAsync(id);
            if (messageType is null) {
                return default;
            }
            return new MessageType {
                Id = messageType.Id,
                Name = messageType.Name
            };
        }

        /// <inheritdoc />
        public async Task<MessageType> GetByName(string name) {
            var messageType = await DbContext.MessageTypes.Where(x => x.Name == name).FirstOrDefaultAsync();
            if (messageType is null) {
                return default;
            }
            return new MessageType {
                Id = messageType.Id,
                Name = messageType.Name
            };
        }

        /// <inheritdoc />
        public Task<ResultSet<MessageType>> GetList(ListOptions options) {
            var query = DbContext
                .MessageTypes
                .AsNoTracking()
                .Select(campaignType => new MessageType {
                    Id = campaignType.Id,
                    Name = campaignType.Name
                });
            if (!string.IsNullOrWhiteSpace(options.Search)) {
                query = query.Where(x => x.Name.ToLower().Contains(options.Search.ToLower()));
            }
            return query.ToResultSetAsync(options);
        }

        /// <inheritdoc />
        public async Task Update(Guid id, UpsertMessageTypeRequest request) {
            var messageType = await DbContext.MessageTypes.FindAsync(id);
            if (messageType is null) {
                throw MessageException.MessageTypeNotFound(id);
            }
            messageType.Name = request.Name;
            await DbContext.SaveChangesAsync();
        }
    }
}
