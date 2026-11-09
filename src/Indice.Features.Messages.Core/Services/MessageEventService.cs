using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Queries;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Models;
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
            MessageId = x.MessageId,
            Recipient = x.Recipient,
            Title = x.Title,
            Success = x.Success
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
            if (!string.IsNullOrWhiteSpace(options.Filter.Recipient)) {
                query = query.Where(x => x.Recipient == options.Filter.Recipient);
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
                             .SqlQueryRaw<MessageEventSeries>(descriptor.RollUpRawSql, descriptor.RollUpParameters(
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
                         Events = leftResult?.Events ?? date.Events,
                     });


        var set = new MessageSeriesResultSet(items, items.Count(), summary);
        return set;
    }

    /// <inheritdoc/>
    public async Task<DeliveryMetrics> GetDeliveryMetricsAsync(DeliveryMetricsFilter filter) {
        var timeFrame = filter.TimeFrame ?? SeriesTimeFrame.Last30Days;
        var now = DateTimeOffset.UtcNow;
        var today = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var (rangeStart, interval) = timeFrame switch {
            SeriesTimeFrame.Last24Hours => (new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero).AddHours(-23), DeliveryInterval.Hour),
            SeriesTimeFrame.Last7Days => (today.AddDays(-6), DeliveryInterval.Day),
            SeriesTimeFrame.Last90Days => (today.AddDays(-89), DeliveryInterval.Day),
            SeriesTimeFrame.Last12Months => (new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(-11), DeliveryInterval.Month),
            _ => (today.AddDays(-29), DeliveryInterval.Day)
        };
        var previousStart = rangeStart - (now - rangeStart);

        // Dispatch attempts: provider results (Sent, successful or not) plus inbox messages, which are delivered on creation.
        var sent = nameof(MessageEventType.Sent);
        var created = nameof(MessageEventType.Created);
        var inbox = nameof(MessageChannelKind.Inbox);
        var attempts = DbContext.MessageEvents.Where(x => x.Type == sent || (x.Channel == inbox && x.Type == created));
        if (filter.Channel.HasValue) {
            var channel = filter.Channel.Value.ToString();
            attempts = attempts.Where(x => x.Channel == channel);
        }
        var current = attempts.Where(x => x.CreatedOn >= rangeStart);

        var buckets = interval switch {
            DeliveryInterval.Hour => await current
                .GroupBy(x => new { x.CreatedOn.Year, x.CreatedOn.Month, x.CreatedOn.Day, x.CreatedOn.Hour, Failed = x.Type == sent && !x.Success })
                .Select(g => new DeliveryBucket(g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, g.Key.Failed, g.Count()))
                .ToListAsync(),
            DeliveryInterval.Month => await current
                .GroupBy(x => new { x.CreatedOn.Year, x.CreatedOn.Month, Failed = x.Type == sent && !x.Success })
                .Select(g => new DeliveryBucket(g.Key.Year, g.Key.Month, 1, 0, g.Key.Failed, g.Count()))
                .ToListAsync(),
            _ => await current
                .GroupBy(x => new { x.CreatedOn.Year, x.CreatedOn.Month, x.CreatedOn.Day, Failed = x.Type == sent && !x.Success })
                .Select(g => new DeliveryBucket(g.Key.Year, g.Key.Month, g.Key.Day, 0, g.Key.Failed, g.Count()))
                .ToListAsync()
        };
        var perChannel = await current
            .GroupBy(x => new { x.Channel, Failed = x.Type == sent && !x.Success })
            .Select(g => new { g.Key.Channel, g.Key.Failed, Count = g.Count() })
            .ToListAsync();
        var previous = await attempts
            .Where(x => x.CreatedOn >= previousStart && x.CreatedOn < rangeStart)
            .GroupBy(x => x.Type == sent && !x.Success)
            .Select(g => new { Failed = g.Key, Count = g.Count() })
            .ToListAsync();

        var items = new List<DeliverySeriesItem>();
        for (var date = rangeStart; date < now; date = interval switch {
            DeliveryInterval.Hour => date.AddHours(1),
            DeliveryInterval.Month => date.AddMonths(1),
            _ => date.AddDays(1)
        }) {
            var matches = buckets.Where(x => x.Date == date).ToList();
            items.Add(new DeliverySeriesItem {
                Date = date,
                Dispatched = matches.Where(x => !x.Failed).Sum(x => x.Count),
                Failed = matches.Where(x => x.Failed).Sum(x => x.Count)
            });
        }
        return new DeliveryMetrics {
            TimeFrame = timeFrame,
            RangeStart = rangeStart,
            RangeEnd = now,
            Interval = interval,
            Items = items,
            Current = new DeliveryTotals {
                Dispatched = buckets.Where(x => !x.Failed).Sum(x => x.Count),
                Failed = buckets.Where(x => x.Failed).Sum(x => x.Count)
            },
            Previous = new DeliveryTotals {
                Dispatched = previous.Where(x => !x.Failed).Sum(x => x.Count),
                Failed = previous.Where(x => x.Failed).Sum(x => x.Count)
            },
            PerChannel = perChannel
                .Where(x => Enum.TryParse<MessageChannelKind>(x.Channel, out _))
                .GroupBy(x => Enum.Parse<MessageChannelKind>(x.Channel))
                .Select(g => new ChannelDeliveryTotals {
                    Kind = g.Key,
                    Dispatched = g.Where(x => !x.Failed).Sum(x => x.Count),
                    Failed = g.Where(x => x.Failed).Sum(x => x.Count)
                })
                .OrderByDescending(x => x.Total)
                .ToList()
        };
    }

    private sealed record DeliveryBucket(int Year, int Month, int Day, int Hour, bool Failed, int Count)
    {
        public DateTimeOffset Date => new(Year, Month, Day, Hour, 0, 0, TimeSpan.Zero);
    }
}
