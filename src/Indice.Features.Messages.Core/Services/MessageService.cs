using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;

namespace Indice.Features.Messages.Core.Services;

/// <summary>An implementation of <see cref="IMessageService"/> for Entity Framework Core.</summary>
public class MessageService : IMessageService
{
    /// <summary>Creates a new instance of <see cref="MessageService"/>.</summary>
    /// <param name="dbContext">The <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for Campaigns API feature.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MessageService(CampaignsDbContext dbContext) {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    private CampaignsDbContext DbContext { get; }

    /// <inheritdoc />
    public async Task<Guid> Create(CreateMessageRequest request) {
        var dbMessage = new DbMessage {
            CampaignId = request.CampaignId,
            ContactId = request.ContactId,
            Content = request.Content,
            Id = Guid.NewGuid(),
            RecipientId = request.RecipientId
        };
        DbContext.Messages.Add(dbMessage);
        await DbContext.SaveChangesAsync();

        return dbMessage.Id;
    }
}
