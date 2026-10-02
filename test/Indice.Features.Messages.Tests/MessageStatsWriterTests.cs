using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Indice.Features.Messages.Tests;

[Collection(MessageStatsCollection.Name)]
public class MessageStatsWriterTests(MessageStatsDatabaseFixture fixture) : IAsyncLifetime
{
    private static readonly DateOnly Day = new(2026, 3, 31);
    private static readonly DateOnly Month = new(2026, 3, 1);
    private static readonly Guid TypeId = Guid.NewGuid();

    public async ValueTask InitializeAsync() => await fixture.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task AddAsync_CreatesDayAndMonthRows_ThenIncrementsBoth() {
        var writer = fixture.CreateWriter();

        Assert.True(await writer.AddAsync([Count(Day, MessageChannelKind.Email, 3, "Marketing")], TestContext.Current.CancellationToken));

        var stats = await fixture.GetStatsAsync();
        Assert.Equal(2, stats.Count);
        Assert.Equal(3, Sends(stats, MessageStatGranularity.Day, Day, MessageChannelKind.Email));
        Assert.Equal(3, Sends(stats, MessageStatGranularity.Month, Month, MessageChannelKind.Email));
        Assert.All(stats, x => Assert.Equal("Marketing", x.CampaignTypeName));

        Assert.True(await writer.AddAsync([
            Count(Day, MessageChannelKind.Email, 2, "Renamed"),
            Count(Day.AddDays(-1), MessageChannelKind.Email, 4, "Renamed"),
            Count(Day, MessageChannelKind.SMS, 1, "Renamed")
        ], TestContext.Current.CancellationToken));

        stats = await fixture.GetStatsAsync();
        Assert.Equal(5, stats.Count);
        Assert.Equal(5, Sends(stats, MessageStatGranularity.Day, Day, MessageChannelKind.Email));
        Assert.Equal(4, Sends(stats, MessageStatGranularity.Day, Day.AddDays(-1), MessageChannelKind.Email));
        Assert.Equal(9, Sends(stats, MessageStatGranularity.Month, Month, MessageChannelKind.Email));
        Assert.Equal(1, Sends(stats, MessageStatGranularity.Day, Day, MessageChannelKind.SMS));
        Assert.Equal(1, Sends(stats, MessageStatGranularity.Month, Month, MessageChannelKind.SMS));
        // The name is captured when the row is first created.
        Assert.Equal("Marketing", stats.Single(x => x.Granularity == MessageStatGranularity.Day && x.PeriodStart == Day && x.Channel == MessageChannelKind.Email).CampaignTypeName);
    }

    [Fact]
    public async Task AddAsync_WritesAtTheSameTime_AddUp() {
        var writer = fixture.CreateWriter();

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => writer.AddAsync([Count(Day, MessageChannelKind.Email, 1)], TestContext.Current.CancellationToken)));

        Assert.All(results, Assert.True);
        var stats = await fixture.GetStatsAsync();
        Assert.Equal(8, Sends(stats, MessageStatGranularity.Day, Day, MessageChannelKind.Email));
        Assert.Equal(8, Sends(stats, MessageStatGranularity.Month, Month, MessageChannelKind.Email));
    }

    [Fact]
    public async Task AddAsync_LockHeldBySomeoneElse_WritesNothing() {
        var writer = fixture.CreateWriter();
        fixture.LockManager.Unavailable = true;

        Assert.False(await writer.AddAsync([Count(Day, MessageChannelKind.Email, 3)], TestContext.Current.CancellationToken));

        Assert.Empty(await fixture.GetStatsAsync());
    }

    [Fact]
    public async Task AddAsync_SaveFails_ReleasesTheLock() {
        var writer = fixture.CreateWriter();
        // The name does not fit in the column, so the save fails.
        var tooLong = new string('x', 200);

        await Assert.ThrowsAsync<DbUpdateException>(() => writer.AddAsync([Count(Day, MessageChannelKind.Email, 3, tooLong)], TestContext.Current.CancellationToken));

        Assert.False(fixture.LockManager.IsHeld);
        Assert.Empty(await fixture.GetStatsAsync());
        Assert.True(await writer.AddAsync([Count(Day, MessageChannelKind.Email, 3)], TestContext.Current.CancellationToken));
    }

    private static MessageStatCount Count(DateOnly day, MessageChannelKind channel, int count, string? campaignTypeName = "Marketing") => new() {
        Day = day,
        Channel = channel,
        CampaignTypeId = TypeId,
        CampaignTypeName = campaignTypeName,
        Count = count
    };

    private static int Sends(List<DbMessageStat> stats, MessageStatGranularity granularity, DateOnly periodStart, MessageChannelKind channel) =>
        stats.Single(x => x.Granularity == granularity && x.PeriodStart == periodStart && x.Channel == channel && x.CampaignTypeId == TypeId).SuccessfulSends;
}
