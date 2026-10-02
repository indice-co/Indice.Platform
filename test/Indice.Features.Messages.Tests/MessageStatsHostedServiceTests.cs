using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services;
using Xunit;

namespace Indice.Features.Messages.Tests;

[Collection(MessageStatsCollection.Name)]
public class MessageStatsHostedServiceTests(MessageStatsDatabaseFixture fixture) : IAsyncLifetime
{
    private static readonly DateOnly Day = new(2026, 3, 30);
    private static readonly DateOnly NextDay = new(2026, 3, 31);
    private static readonly DateOnly Month = new(2026, 3, 1);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Flush_SumsByDayChannelAndCampaignType() {
        var typeId = await fixture.AddMessageTypeAsync("Marketing");
        var campaignA = await fixture.AddCampaignAsync(typeId);
        var campaignB = await fixture.AddCampaignAsync(typeId);
        var campaignWithoutType = await fixture.AddCampaignAsync();
        var globalCampaign = await fixture.AddCampaignAsync(typeId, isGlobal: true);
        var unknownCampaign = Guid.NewGuid();
        var service = fixture.CreateHostedService();
        Add(service, campaignA, MessageChannelKind.Email, Day, 2);
        Add(service, campaignB, MessageChannelKind.Email, Day, 1);
        Add(service, campaignA, MessageChannelKind.SMS, Day, 1);
        Add(service, campaignA, MessageChannelKind.Email, NextDay, 1);
        Add(service, campaignWithoutType, MessageChannelKind.Email, Day, 1);
        Add(service, unknownCampaign, MessageChannelKind.Email, Day, 2);
        Add(service, globalCampaign, MessageChannelKind.PushNotification, Day, 5);

        Assert.True(await service.FlushAsync(TestContext.Current.CancellationToken));

        var stats = await fixture.GetStatsAsync();
        Assert.Equal(7, stats.Count);
        Assert.Equal(3, stats.Single(x => x.Granularity == MessageStatGranularity.Day && x.PeriodStart == Day && x.Channel == MessageChannelKind.Email && x.CampaignTypeId == typeId).SuccessfulSends);
        Assert.Equal(1, stats.Single(x => x.Granularity == MessageStatGranularity.Day && x.PeriodStart == NextDay && x.Channel == MessageChannelKind.Email && x.CampaignTypeId == typeId).SuccessfulSends);
        Assert.Equal(4, stats.Single(x => x.Granularity == MessageStatGranularity.Month && x.PeriodStart == Month && x.Channel == MessageChannelKind.Email && x.CampaignTypeId == typeId).SuccessfulSends);
        Assert.Equal(1, stats.Single(x => x.Granularity == MessageStatGranularity.Day && x.PeriodStart == Day && x.Channel == MessageChannelKind.SMS && x.CampaignTypeId == typeId).SuccessfulSends);
        Assert.Equal(1, stats.Single(x => x.Granularity == MessageStatGranularity.Month && x.PeriodStart == Month && x.Channel == MessageChannelKind.SMS && x.CampaignTypeId == typeId).SuccessfulSends);
        // The campaign without a type and the campaign that does not exist are both unclassified.
        Assert.Equal(3, stats.Single(x => x.Granularity == MessageStatGranularity.Day && x.PeriodStart == Day && x.Channel == MessageChannelKind.Email && x.CampaignTypeId == Guid.Empty).SuccessfulSends);
        Assert.Equal(3, stats.Single(x => x.Granularity == MessageStatGranularity.Month && x.PeriodStart == Month && x.Channel == MessageChannelKind.Email && x.CampaignTypeId == Guid.Empty).SuccessfulSends);
        // The global campaign adds nothing.
        Assert.DoesNotContain(stats, x => x.Channel == MessageChannelKind.PushNotification);
        Assert.All(stats.Where(x => x.CampaignTypeId == typeId), x => Assert.Equal("Marketing", x.CampaignTypeName));
        Assert.All(stats.Where(x => x.CampaignTypeId == Guid.Empty), x => Assert.Null(x.CampaignTypeName));
    }

    [Fact]
    public async Task Flush_LockNotAcquired_CarriesTheSumsToTheNextFlush() {
        var campaign = await fixture.AddCampaignAsync();
        var service = fixture.CreateHostedService();
        fixture.LockManager.Unavailable = true;
        Add(service, campaign, MessageChannelKind.Email, Day, 2);

        Assert.False(await service.FlushAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.GetStatsAsync());

        fixture.LockManager.Unavailable = false;
        Add(service, campaign, MessageChannelKind.Email, Day, 1);
        Assert.True(await service.FlushAsync(TestContext.Current.CancellationToken));
        // Nothing is left to write, so one more flush changes nothing.
        Assert.True(await service.FlushAsync(TestContext.Current.CancellationToken));

        var stats = await fixture.GetStatsAsync();
        Assert.Equal(3, stats.Single(x => x.Granularity == MessageStatGranularity.Day).SuccessfulSends);
        Assert.Equal(3, stats.Single(x => x.Granularity == MessageStatGranularity.Month).SuccessfulSends);
    }

    [Fact]
    public async Task Flush_FailsTooManyTimesInARow_DropsTheSums() {
        var campaign = await fixture.AddCampaignAsync();
        var service = fixture.CreateHostedService();
        fixture.LockManager.Unavailable = true;
        Add(service, campaign, MessageChannelKind.Email, Day, 2);
        for (var i = 0; i < MessageStatsDatabaseFixture.MaxCarriedFlushes; i++) {
            Assert.False(await service.FlushAsync(TestContext.Current.CancellationToken));
        }

        fixture.LockManager.Unavailable = false;
        Assert.True(await service.FlushAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.GetStatsAsync());

        // The service keeps working after the drop.
        Add(service, campaign, MessageChannelKind.Email, Day, 1);
        Assert.True(await service.FlushAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, (await fixture.GetStatsAsync()).Single(x => x.Granularity == MessageStatGranularity.Day).SuccessfulSends);
    }

    [Fact]
    public async Task Service_WritesWhatIsQueued_WhenTheHostStops() {
        var campaign = await fixture.AddCampaignAsync();
        var queue = new MessageStatsQueue(fixture.Options);
        var service = fixture.CreateHostedService(queue);
        await service.StartAsync(TestContext.Current.CancellationToken);
        for (var i = 0; i < 3; i++) {
            queue.TryEnqueue(new MessageStatDelta { CampaignId = campaign, Channel = MessageChannelKind.Email, Day = Day });
        }
        // Wait until the service has taken the three sends from the queue.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        while (queue.Reader.Count > 0) {
            await Task.Delay(10, timeout.Token);
        }
        Assert.Empty(await fixture.GetStatsAsync());

        // The flush interval has not passed. Stopping must still write the three sends.
        await service.StopAsync(TestContext.Current.CancellationToken);

        var stats = await fixture.GetStatsAsync();
        Assert.Equal(3, stats.Single(x => x.Granularity == MessageStatGranularity.Day).SuccessfulSends);
        Assert.Equal(3, stats.Single(x => x.Granularity == MessageStatGranularity.Month).SuccessfulSends);
    }

    private static void Add(MessageStatsHostedService service, Guid campaignId, MessageChannelKind channel, DateOnly day, int count) {
        for (var i = 0; i < count; i++) {
            service.Add(new MessageStatDelta { CampaignId = campaignId, Channel = channel, Day = day });
        }
    }
}
