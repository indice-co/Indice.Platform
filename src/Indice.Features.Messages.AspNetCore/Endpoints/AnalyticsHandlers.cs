using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Kpis;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Messages.AspNetCore.Endpoints;

internal static class AnalyticsHandlers
{
    public static async Task<Ok<OverviewMetrics>> GetOverview(IServiceScopeFactory scopeFactory, DateTimeOffset? asOfDate) {
        // Each metric aggregates a large table independently of the others, so they run concurrently, each on its own scope (and DbContext).
        async Task<T> Run<T>(Func<IServiceProvider, Task<T>> query) {
            await using var scope = scopeFactory.CreateAsyncScope();
            return await query(scope.ServiceProvider);
        }
        var campaign = Run(sp => sp.GetRequiredService<ICampaignService>().GetMetrics(asOfDate));
        var contact = Run(async sp => {
            var contactService = sp.GetRequiredService<IContactService>();
            return new ContactMetrics {
                Total = (await contactService.GetList(new() { Page = 1, Size = 0 })).Count,
                Known = (await contactService.GetList(new() { Page = 1, Size = 0, Filter = new() { Anonymous = false } })).Count
            };
        });
        var recipient = Run(sp => sp.GetRequiredService<ICampaignService>().GetRecipientMetrics());
        var perChannel = Run(sp => sp.GetRequiredService<ICampaignService>().GetChannelMetrics());
        var perType = Run(sp => sp.GetRequiredService<ICampaignService>().GetMessageTypeMetrics(limit: 5));
        var perTypeToday = Run(sp => sp.GetRequiredService<ICampaignService>().GetMessageTypeMetrics(asOfDate ?? DateTimeOffset.UtcNow, limit: 5));
        await Task.WhenAll(campaign, contact, recipient, perChannel, perType, perTypeToday);
        var metrics = new OverviewMetrics {
            Campaign = campaign.Result,
            Contact = contact.Result,
            Recipient = recipient.Result!,
            PerChannel = perChannel.Result,
            PerType = perType.Result,
            PerTypeToday = perTypeToday.Result,
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

    public static async Task<Ok<DeliveryMetrics>> GetDeliveryMetrics(
        IMessageEventService messageEventService,
        [AsParameters] DeliveryMetricsFilter filter) {
        var result = await messageEventService.GetDeliveryMetricsAsync(filter);
        return TypedResults.Ok(result);
    }

    public static async Task<NoContent> RefreshCache(IOutputCacheStore cacheStore,CancellationToken cancellationToken) {
        await cacheStore.EvictByTagAsync(CacheTag, cancellationToken);
        return TypedResults.NoContent();
    }
    internal const string CacheTag = "Analytics";
}
