using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Exceptions;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Messages.Core.Services;

/// <summary>An implementation of <see cref="IMessageSenderService"/> for Entity Framework Core.</summary>
public class MessageSenderService : IMessageSenderService
{
    /// <summary>Creates a new instance of <see cref="MessageSenderService"/>.</summary>
    /// <param name="dbContext">The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for Campaigns API feature.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MessageSenderService(CampaignsDbContext dbContext) {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    private CampaignsDbContext DbContext { get; }

    /// <inheritdoc />
    public async Task<MessageSender> Create(CreateMessageSenderRequest request) {
        var messageSender = new DbMessageSender {
            Id = Guid.NewGuid(),
            Sender = request.Sender,
            DisplayName = request.DisplayName,
            IsDefault = request.IsDefault
        };
        DbContext.MessageSenders.Add(messageSender);
        if (request.IsDefault) {
            var defaultSender = await DbContext.MessageSenders.FirstOrDefaultAsync(s => s.IsDefault);
            if (defaultSender != null) {
                defaultSender.IsDefault = false;
            }
        }
        await DbContext.SaveChangesAsync();
        return new MessageSender {
            Id = messageSender.Id,
            Sender = messageSender.Sender,
            DisplayName = messageSender.DisplayName,
            CreatedAt = messageSender.CreatedAt,
            CreatedBy = messageSender.CreatedBy,
            UpdatedBy = messageSender.UpdatedBy,
            UpdatedAt = messageSender.UpdatedAt,
            IsDefault = messageSender.IsDefault
        };
    }

    /// <inheritdoc />
    public async Task Delete(Guid id) {
        var messageSender = await DbContext.MessageSenders.FindAsync(id) ?? throw MessageExceptions.MessageSenderNotFound(id);
        DbContext.MessageSenders.Remove(messageSender);
        await DbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<MessageSender?> GetById(Guid id) {
        var messageSender = await DbContext.MessageSenders.FindAsync(id);
        if (messageSender is null) {
            return default;
        }
        return new MessageSender {
            Id = messageSender.Id,
            Sender = messageSender.Sender,
            DisplayName = messageSender.DisplayName,
            CreatedAt = messageSender.CreatedAt,
            CreatedBy = messageSender.CreatedBy,
            UpdatedBy = messageSender.UpdatedBy,
            UpdatedAt = messageSender.UpdatedAt,
            IsDefault = messageSender.IsDefault
        };
    }

    /// <inheritdoc />
    public async Task<MessageSender?> GetByName(string name) {
        var messageSender = await DbContext.MessageSenders.Where(x => x.DisplayName == name).FirstOrDefaultAsync();
        if (messageSender is null) {
            return default;
        }
        return new MessageSender {
            Id = messageSender.Id,
            Sender = messageSender.Sender,
            DisplayName = messageSender.DisplayName,
            CreatedAt = messageSender.CreatedAt,
            CreatedBy = messageSender.CreatedBy,
            UpdatedBy = messageSender.UpdatedBy,
            UpdatedAt = messageSender.UpdatedAt,
            IsDefault = messageSender.IsDefault
        };
    }

    /// <inheritdoc />
    public Task<ResultSet<MessageSender>> GetList(ListOptions options, MessageSenderListFilter filter) {
        var query = DbContext
            .MessageSenders
            .AsNoTracking();
        if (!string.IsNullOrWhiteSpace(options.Search)) {
            query = query.Where(x => x.DisplayName!.Contains(options.Search, StringComparison.CurrentCultureIgnoreCase));
        }
        if (filter?.IsDefault is not null) {
            query = query.Where(x => x.IsDefault == filter.IsDefault.Value);
        }
        return query.Select(messageSender => new MessageSender {
            Id = messageSender.Id,
            Sender = messageSender.Sender,
            DisplayName = messageSender.DisplayName,
            CreatedAt = messageSender.CreatedAt,
            CreatedBy = messageSender.CreatedBy,
            UpdatedBy = messageSender.UpdatedBy,
            UpdatedAt = messageSender.UpdatedAt,
            IsDefault = messageSender.IsDefault
        }).ToResultSetAsync(options);
    }

    /// <inheritdoc />
    public async Task Update(Guid id, UpdateMessageSenderRequest request) {
        var messageSender = await DbContext.MessageSenders.FindAsync(id) ?? throw MessageExceptions.MessageSenderNotFound(id);
        messageSender.Sender = request.Sender;
        messageSender.DisplayName = request.DisplayName;
        if (request.IsDefault && !messageSender.IsDefault) {
            var defaultSender = await DbContext.MessageSenders.FirstOrDefaultAsync(s => s.IsDefault);
            if (defaultSender != null) {
                defaultSender.IsDefault = false;
                messageSender.IsDefault = true;
            }
        }
        await DbContext.SaveChangesAsync();
    }
}
