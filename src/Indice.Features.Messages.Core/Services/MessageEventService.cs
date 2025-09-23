using System.Linq;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Queries;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Models.Kpis;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

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
                query = query.Where(x => x.CampaignId == options.Filter.CampaignId.Value);
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

    /// <inheritdoc/>
    public async Task<MessageSeriesResultSet> GetSeriesAsync(MessageEventSeriesFilter filter) {
        var rangeStart = filter.TimeFrame switch {
            SeriesTimeFrame.Last24Hours => DateTimeOffset.UtcNow.AddHours(-24),
            SeriesTimeFrame.Last7Days => DateTimeOffset.UtcNow.Date.AddDays(-7),
            SeriesTimeFrame.Last30Days => DateTimeOffset.UtcNow.Date.AddDays(-30),
            SeriesTimeFrame.Last90Days => DateTimeOffset.UtcNow.Date.AddDays(-90),
            SeriesTimeFrame.Last12Months => DateTimeOffset.UtcNow.Date.AddMonths(-12),
            _ => DateTimeOffset.UtcNow.Date.AddDays(-7)
        };

        var allDates = Enumerable.Range(0, (DateTimeOffset.UtcNow.Date - rangeStart).Days + 1)
                              .Select(offset => {
                                  var date = rangeStart.Date.AddDays(offset);
                                  return new MessageEventSeries {
                                      Year = date.Year,
                                      Month = date.Month,
                                      Day = date.Day,
                                      Events = 0
                                  };
                              })
                              .ToArray();


        var descriptor = new MessageEventsQueryDescriptor(DbContext);
        var query = DbContext.Database
                             .SqlQuery<MessageEventSeries>(descriptor.RollUp(
                                 type: filter.EventType ?? "Created",
                                 channelKind: filter.Channel,
                                 rangeStart: rangeStart));



        var results = await query.ToListAsync();
        var summary = results.Where(x => x.IsGrandTotal).Select(x => new MessageEventSeriesSummary() { Total = x.Events }).FirstOrDefault(new MessageEventSeriesSummary());
        var dates = results.Where(x => !x.IsTotal);
        var items = (from date in allDates
                     join result in dates on date.Label equals result.Label into leftResults
                     from leftResult in leftResults.DefaultIfEmpty()
                     select new MessageEventSeries {
                         Day = leftResult?.Day ?? date.Day,
                         Month = leftResult?.Month ?? date.Month,
                         Year = leftResult?.Year ?? date.Year,
                     });


        var set = new MessageSeriesResultSet(items, items.Count(), summary);
        return set;
    }
}
