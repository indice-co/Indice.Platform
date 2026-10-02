using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Indice.Features.Messages.Tests;

[Collection(MessageStatsCollection.Name)]
public class MessageStatsVerificationTests(MessageStatsDatabaseFixture fixture) : IAsyncLifetime
{
    // The verification looks back two complete days: the 2nd and the 1st of March.
    private static readonly DateOnly Today = new(2026, 3, 3);
    private static readonly DateOnly Yesterday = new(2026, 3, 2);
    private static readonly DateOnly TwoDaysAgo = new(2026, 3, 1);
    private static readonly DateOnly OutsideLookback = new(2026, 2, 28);
    private static readonly DateOnly March = new(2026, 3, 1);
    private static readonly DateOnly February = new(2026, 2, 1);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Verify_CorrectsTheDayRows_AndRebuildsTheMonthRows() {
        var typeId = await SeedAsync();

        await VerifyAsync();

        var stats = await fixture.GetStatsAsync();
        // A low row is raised, a high row is lowered, a missing row is inserted and a row with no events is deleted.
        Assert.Equal(3, Sends(stats, MessageStatGranularity.Day, Yesterday, MessageChannelKind.Email, typeId));
        Assert.Equal(2, Sends(stats, MessageStatGranularity.Day, Yesterday, MessageChannelKind.Inbox, typeId));
        Assert.Equal(1, Sends(stats, MessageStatGranularity.Day, Yesterday, MessageChannelKind.SMS, Guid.Empty));
        Assert.DoesNotContain(stats, x => x.Channel == MessageChannelKind.PushNotification);
        Assert.Equal(1, Sends(stats, MessageStatGranularity.Day, TwoDaysAgo, MessageChannelKind.Email, typeId));
        // The month is the sum of its day rows, with the running day as it stands.
        Assert.Equal(5, Sends(stats, MessageStatGranularity.Month, March, MessageChannelKind.Email, typeId));
        Assert.Equal(2, Sends(stats, MessageStatGranularity.Month, March, MessageChannelKind.Inbox, typeId));
        Assert.Equal(1, Sends(stats, MessageStatGranularity.Month, March, MessageChannelKind.SMS, Guid.Empty));
        // A row that the verification inserts gets the name of the campaign type.
        Assert.Equal(1, Sends(stats, MessageStatGranularity.Day, TwoDaysAgo, MessageChannelKind.SMS, typeId));
        Assert.Equal(1, Sends(stats, MessageStatGranularity.Month, March, MessageChannelKind.SMS, typeId));
        Assert.All(stats.Where(x => x.Channel == MessageChannelKind.SMS && x.CampaignTypeId == typeId), x => Assert.Equal("Marketing", x.CampaignTypeName));
        Assert.Equal(12, stats.Count);
    }

    [Fact]
    public async Task Verify_LeavesTheRunningDay_AndTheDaysOutsideTheLookback() {
        var typeId = await SeedAsync();

        await VerifyAsync();

        var stats = await fixture.GetStatsAsync();
        // The running day has four saved events but its row is not recounted.
        Assert.Equal(1, Sends(stats, MessageStatGranularity.Day, Today, MessageChannelKind.Email, typeId));
        // The day outside the lookback has no events at all and its rows are kept.
        Assert.Equal(7, Sends(stats, MessageStatGranularity.Day, OutsideLookback, MessageChannelKind.Email, typeId));
        Assert.Equal(7, Sends(stats, MessageStatGranularity.Month, February, MessageChannelKind.Email, typeId));
    }

    [Fact]
    public async Task Verify_RunTwice_ChangesNothingTheSecondTime() {
        await SeedAsync();
        await VerifyAsync();
        var first = await fixture.GetStatsAsync();

        await VerifyAsync();

        var second = await fixture.GetStatsAsync();
        Assert.Equal(Describe(first), Describe(second));
    }

    [Fact]
    public async Task Verify_LockHeldBySomeoneElse_ChangesNothing() {
        await SeedAsync();
        var before = await fixture.GetStatsAsync();
        fixture.LockManager.Unavailable = true;

        await VerifyAsync();

        Assert.Equal(Describe(before), Describe(await fixture.GetStatsAsync()));
    }

    private async Task VerifyAsync() {
        using var scope = fixture.ServiceProvider.CreateScope();
        var service = new MessageStatsVerificationService(fixture.Options, scope.ServiceProvider.GetRequiredService<CampaignsDbContext>(), fixture.CreateWriter(), NullLogger<MessageStatsVerificationService>.Instance);
        await service.VerifyAsync(Today, TestContext.Current.CancellationToken);
    }

    private async Task<Guid> SeedAsync() {
        var typeId = await fixture.AddMessageTypeAsync("Marketing");
        var campaign = await fixture.AddCampaignAsync(typeId);
        var globalCampaign = await fixture.AddCampaignAsync(typeId, isGlobal: true);
        var unknownCampaign = Guid.NewGuid();
        // Yesterday, from the first to the last second of the day.
        await fixture.AddEventsAsync(campaign, "Email", "Sent", At(Yesterday, 0, 0, 0));
        await fixture.AddEventsAsync(campaign, "Email", "Sent", At(Yesterday, 12, 0, 0));
        await fixture.AddEventsAsync(campaign, "Email", "Sent", At(Yesterday, 23, 59, 59));
        await fixture.AddEventsAsync(campaign, "Email", "Sent", At(Yesterday, 12, 0, 0), success: false);
        await fixture.AddEventsAsync(campaign, "Email", "Created", At(Yesterday, 12, 0, 0));
        await fixture.AddEventsAsync(campaign, "Inbox", "Created", At(Yesterday, 12, 0, 0), count: 2);
        await fixture.AddEventsAsync(campaign, "Inbox", "Read", At(Yesterday, 12, 0, 0));
        await fixture.AddEventsAsync(unknownCampaign, "SMS", "Sent", At(Yesterday, 12, 0, 0));
        // The broadcast of a global campaign is not counted.
        await fixture.AddEventsAsync(globalCampaign, "PushNotification", "Sent", At(Yesterday, 12, 0, 0));
        await fixture.AddEventsAsync(campaign, "Email", "Sent", At(TwoDaysAgo, 12, 0, 0));
        await fixture.AddEventsAsync(campaign, "SMS", "Sent", At(TwoDaysAgo, 12, 0, 0));
        await fixture.AddEventsAsync(campaign, "Email", "Sent", At(Today, 0, 0, 0), count: 4);

        await fixture.AddStatAsync(MessageStatGranularity.Day, Yesterday, MessageChannelKind.Email, typeId, 1);
        await fixture.AddStatAsync(MessageStatGranularity.Day, Yesterday, MessageChannelKind.Inbox, typeId, 5);
        await fixture.AddStatAsync(MessageStatGranularity.Day, Yesterday, MessageChannelKind.PushNotification, typeId, 9);
        await fixture.AddStatAsync(MessageStatGranularity.Day, TwoDaysAgo, MessageChannelKind.Email, typeId, 1);
        await fixture.AddStatAsync(MessageStatGranularity.Day, Today, MessageChannelKind.Email, typeId, 1);
        await fixture.AddStatAsync(MessageStatGranularity.Month, March, MessageChannelKind.Email, typeId, 99);
        await fixture.AddStatAsync(MessageStatGranularity.Month, March, MessageChannelKind.PushNotification, typeId, 9);
        await fixture.AddStatAsync(MessageStatGranularity.Day, OutsideLookback, MessageChannelKind.Email, typeId, 7);
        await fixture.AddStatAsync(MessageStatGranularity.Month, February, MessageChannelKind.Email, typeId, 7);
        return typeId;
    }

    private static DateTimeOffset At(DateOnly day, int hour, int minute, int second) => new(day.Year, day.Month, day.Day, hour, minute, second, TimeSpan.Zero);

    private static int Sends(List<DbMessageStat> stats, MessageStatGranularity granularity, DateOnly periodStart, MessageChannelKind channel, Guid campaignTypeId) =>
        stats.Single(x => x.Granularity == granularity && x.PeriodStart == periodStart && x.Channel == channel && x.CampaignTypeId == campaignTypeId).SuccessfulSends;

    // The rows as text, with the time of the last write, so that two readings can be compared.
    private static List<string> Describe(List<DbMessageStat> stats) => stats
        .Select(x => $"{x.Granularity} {x.PeriodStart:yyyy-MM-dd} {x.Channel} {x.CampaignTypeId} {x.SuccessfulSends} {x.UpdatedAt:O}")
        .Order()
        .ToList();
}
