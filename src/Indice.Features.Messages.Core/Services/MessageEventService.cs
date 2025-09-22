using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Org.BouncyCastle.Security.Certificates;

namespace Indice.Features.Messages.Core.Services;

/// <inheritdoc/>
public class MessageEventService : IMessageEventService
{
    /// <summary>Creates a new instance of <see cref="MessageEventService"/>.</summary>
    public MessageEventService(CampaignsDbContext dbContext) {
        DbContext = dbContext;
    }

    /// <summary>The campaigns database context.</summary>
    protected CampaignsDbContext DbContext { get; }

    /// <inheritdoc/>
    public Task<ResultSet<MessageEvent>> GetListAsync(ListOptions<MessageEventListFilter> options) {
        var query = DbContext.MessageEvents.Select(x => new MessageEvent {
            Id = x.Id,
            CampaignId = x.CampaignId,
            Channel = x.Channel,
            Type = x.Type,
            ContactId = x.ContactId,
            CreatedOn = x.CreatedOn,
            MessageId = x.MessageId
        });
        if (options.Filter is not null) { 
            if (options.Filter.CampaignId.HasValue) {
                query= query.Where(x => x.CampaignId == options.Filter.CampaignId.Value);
            }
            if (options.Filter.MessageId.HasValue) {
                query = query.Where(x => x.MessageId == options.Filter.MessageId.Value);
            }
            if (options.Filter.RangeStart.HasValue) {
                query = query.Where(x => x.CreatedOn >= options.Filter.RangeStart.Value);
            }
            if (options.Filter.RangeEnd.HasValue) {
                query = query.Where(x => x.CreatedOn < options.Filter.RangeEnd.Value);
            }
            if (options.Filter.Channel?.Length > 0) {
                var channels = options.Filter.Channel.Select(x => x.ToString());
                query = query.Where(x => channels.Contains(x.Channel));
            }
        }
        var term = options.Search?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(term)) {
            query = query.Where(x => x.Type.ToLower().Contains(term));
        }
        return query.ToResultSetAsync(options);
    }
}
