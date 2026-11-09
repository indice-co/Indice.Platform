using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Exceptions;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Messages.Core.Services;

/// <summary>An implementation of <see cref="IMessageTypeService"/> for Entity Framework Core.</summary>
public class MessageTypeService : IMessageTypeService
{
    /// <summary>Creates a new instance of <see cref="MessageTypeService"/>.</summary>
    /// <param name="dbContext">The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for Campaigns API feature.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MessageTypeService(CampaignsDbContext dbContext) {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    private CampaignsDbContext DbContext { get; }

    /// <inheritdoc />
    public async Task<MessageType> Create(CreateMessageTypeRequest request) {
        var messageType = new DbMessageType {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Alias = string.IsNullOrWhiteSpace(request.Alias) ? null : request.Alias.Trim(),
            Classification = request.Classification
        };
        DbContext.MessageTypes.Add(messageType);
        await DbContext.SaveChangesAsync();
        return new MessageType {
            Id = messageType.Id,
            Name = messageType.Name,
            Alias = messageType.Alias,
            Classification = messageType.Classification,
        };
    }

    /// <inheritdoc />
    public async Task Delete(Guid id) {
        var messageType = await DbContext.MessageTypes.FindAsync(id) ?? throw MessageExceptions.MessageTypeNotFound(id);
        DbContext.MessageTypes.Remove(messageType);
        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<MessageType?> GetById(GuidOrAlias? id) {
        if (id is null || string.IsNullOrWhiteSpace(id.Value.Value)) {
            return default;
        }

        var idValue = id.Value;
        DbMessageType? messageType = idValue.IsGuid ?
            await DbContext.MessageTypes.FindAsync(idValue.Uuid) :
            await DbContext.MessageTypes.FirstOrDefaultAsync(x => x.Alias == idValue.Value);

        if (messageType is null) {
            return default;
        }
        return new MessageType {
            Id = messageType.Id,
            Name = messageType.Name,
            Alias = messageType.Alias,
            Classification = messageType.Classification
        };
    }

    /// <inheritdoc />
    public async Task<MessageType?> GetByName(string name) {
        var messageType = await DbContext.MessageTypes.Where(x => x.Name == name.Trim()).FirstOrDefaultAsync();
        if (messageType is null) {
            return default;
        }
        return new MessageType {
            Id = messageType.Id,
            Name = messageType.Name,
            Alias = messageType.Alias,
            Classification = messageType.Classification
        };
    }

    /// <inheritdoc />
    public Task<ResultSet<MessageType>> GetList(ListOptions options) {
        var query = DbContext
            .MessageTypes
            .AsNoTracking()
            .Select(campaignType => new MessageType {
                Id = campaignType.Id,
                Name = campaignType.Name,
                Alias = campaignType.Alias,
                Classification = campaignType.Classification
            });
        if (!string.IsNullOrWhiteSpace(options.Search) && options.Search.Length > 2) {
            query = query.Where(x =>
            x.Name!.ToLower().Contains(options.Search.ToLower()) ||
            x.Alias!.ToLower().Contains(options.Search.ToLower())
            );
        }
        return query.ToResultSetAsync(options);
    }

    /// <inheritdoc />
    public async Task Update(Guid id, UpdateMessageTypeRequest request) {
        var messageType = await DbContext.MessageTypes.FindAsync(id) ?? throw MessageExceptions.MessageTypeNotFound(id);
        messageType.Name = request.Name;
        messageType.Classification = request.Classification;
        messageType.Alias = string.IsNullOrWhiteSpace(request.Alias) ? null : request.Alias.Trim();
        if (!string.IsNullOrWhiteSpace(messageType.Alias)) {
            var existingAlias = await GetById((GuidOrAlias)messageType.Alias);
            if (existingAlias != null && existingAlias.Id != id) {
                throw MessageExceptions.MessageTypeAliasExists(messageType.Alias);
            }
        }
        await DbContext.SaveChangesAsync();
    }
}