using System.Globalization;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace Indice.Features.Messages.Tests;

public class MessageStatsEligibilityTests
{
    [Theory]
    [InlineData("Email", "Sent", MessageChannelKind.Email)]
    [InlineData("SMS", "Sent", MessageChannelKind.SMS)]
    [InlineData("PushNotification", "Sent", MessageChannelKind.PushNotification)]
    [InlineData("Inbox", "Created", MessageChannelKind.Inbox)]
    public void SuccessfulSend_IsCounted(string channel, string type, MessageChannelKind expectedChannel) {
        var campaignId = Guid.NewGuid();
        var delta = MessageStatsEligibility.ToDelta(CreateEvent(channel, type, success: true, campaignId: campaignId));
        Assert.NotNull(delta);
        Assert.Equal(expectedChannel, delta.Channel);
        Assert.Equal(campaignId, delta.CampaignId);
    }

    [Theory]
    [InlineData("Email", "Sent")]
    [InlineData("SMS", "Sent")]
    [InlineData("PushNotification", "Sent")]
    [InlineData("Inbox", "Created")]
    public void FailedSend_IsNotCounted(string channel, string type) =>
        Assert.Null(MessageStatsEligibility.ToDelta(CreateEvent(channel, type, success: false)));

    [Theory]
    // Created counts for Inbox only. The other channels count when they are sent.
    [InlineData("Email", "Created")]
    [InlineData("SMS", "Created")]
    [InlineData("PushNotification", "Created")]
    [InlineData("Inbox", "Sent")]
    [InlineData("Inbox", "Read")]
    [InlineData("Inbox", "UnRead")]
    [InlineData("Inbox", "Deleted")]
    [InlineData("Inbox", "Opened")]
    [InlineData("Email", "Opened")]
    [InlineData("Email", "Delivered")]
    [InlineData("SMS", "Delivered")]
    // A channel name that is not one of the four channels.
    [InlineData("Fax", "Sent")]
    [InlineData("", "Sent")]
    [InlineData("None", "Sent")]
    [InlineData("Inbox, Email", "Sent")]
    public void OtherEvents_AreNotCounted(string channel, string type) =>
        Assert.Null(MessageStatsEligibility.ToDelta(CreateEvent(channel, type, success: true)));

    [Theory]
    [InlineData("2026-01-31T23:59:59+00:00", "2026-01-31", "2026-01-01")]
    [InlineData("2026-02-01T00:00:00+00:00", "2026-02-01", "2026-02-01")]
    // The day is the UTC day, whatever the offset of the timestamp.
    [InlineData("2026-02-01T01:30:00+02:00", "2026-01-31", "2026-01-01")]
    [InlineData("2026-01-31T22:30:00-02:00", "2026-02-01", "2026-02-01")]
    public void Day_AndMonth_AreTakenFromTheEventTimeInUtc(string createdOn, string expectedDay, string expectedMonth) {
        var delta = MessageStatsEligibility.ToDelta(CreateEvent("Email", "Sent", success: true, createdOn: DateTimeOffset.Parse(createdOn, CultureInfo.InvariantCulture)));
        Assert.NotNull(delta);
        Assert.Equal(DateOnly.Parse(expectedDay, CultureInfo.InvariantCulture), delta.Day);
        Assert.Equal(DateOnly.Parse(expectedMonth, CultureInfo.InvariantCulture), MessageStatsEligibility.MonthOf(delta.Day));
    }

    [Fact]
    public void Queue_DropsWhenFull_AndCountsTheDrops() {
        var queue = new MessageStatsQueue(Options.Create(new AnalyticsOptions { Stats = new MessageStatsOptions { ChannelCapacity = 2 } }));
        var delta = new MessageStatDelta { CampaignId = Guid.NewGuid(), Channel = MessageChannelKind.Email, Day = new DateOnly(2026, 1, 31) };
        Assert.True(queue.TryEnqueue(delta));
        Assert.True(queue.TryEnqueue(delta));
        // The queue is full. The call returns at once instead of waiting for room.
        Assert.False(queue.TryEnqueue(delta));
        Assert.False(queue.TryEnqueue(delta));
        Assert.Equal(2, queue.DroppedCount);
        Assert.True(queue.Reader.TryRead(out _));
        Assert.True(queue.TryEnqueue(delta));
        Assert.Equal(2, queue.DroppedCount);
    }

    private static MessageEvent CreateEvent(string channel, string type, bool success, Guid? campaignId = null, DateTimeOffset? createdOn = null) => new() {
        CampaignId = campaignId ?? Guid.NewGuid(),
        ContactId = Guid.NewGuid(),
        Channel = channel,
        Type = type,
        Success = success,
        CreatedOn = createdOn ?? DateTimeOffset.UtcNow
    };
}
