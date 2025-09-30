using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Kpis;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Messages.AspNetCore.Endpoints;

internal static class AnalyticsHandlers
{
    public static async Task<Ok<OverviewMetrics>> GetOverview(ICampaignService campaignService, IContactService contactService, DateTimeOffset? asOfDate) {
        var metrics = new OverviewMetrics {
            Campaign = (await campaignService.GetMetrics(asOfDate)),
            Contact = new ContactMetrics {
                Total = (await contactService.GetList(new() { Page = 1, Size = 0 })).Count,
                Known = (await contactService.GetList(new() { Page = 1, Size = 0, Filter = new() { Anonymous = false } })).Count
            },
            Recipient = (await campaignService.GetRecipientMetrics())!,
            Channels = (await campaignService.GetChannelMetrics()).Select(x => new ChannelMetrics { 
                Kind = Enum.Parse<MessageChannelKind>(x.Key), 
                Total = x.Value 
            }).ToList(),
            LastUpdateDate = DateTimeOffset.UtcNow
        };
        return TypedResults.Ok(metrics);
    }

    public static async Task<Ok<ResultSet<MessageEvent>>> GetEventsList(
        IMessageEventService messageEventService,
        [AsParameters]ListOptions listOptions,
        [AsParameters] MessageEventListFilter filter) {
        var result = await messageEventService.GetListAsync(ListOptions.Create(listOptions, filter));
        return TypedResults.Ok(result);
    }

    public static async Task<Ok<MessageSeriesResultSet>> GetEventsSeriesList(
        IMessageEventService messageEventService,
        [AsParameters] MessageEventSeriesFilter filter) {
        var result = await messageEventService.GetSeriesAsync(filter);
        return TypedResults.Ok(result);
    }
}
