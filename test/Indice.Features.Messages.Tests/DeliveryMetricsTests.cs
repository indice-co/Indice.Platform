using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Kpis;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Indice.Features.Messages.Tests;

public class DeliveryMetricsTests : IAsyncLifetime
{
    private readonly ServiceProvider _serviceProvider;

    public DeliveryMetricsTests() {
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=MessagesDb.DeliveryTest_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true";
        _serviceProvider = new ServiceCollection()
            .AddDbContext<CampaignsDbContext>(builder => builder.UseSqlServer(connectionString))
            .AddTransient(serviceProvider => new DatabaseSchemaNameResolver("cmp"))
            .AddTransient<IUserNameAccessor, UserNameAccessorNoOp>()
            .AddTransient<UserNameAccessorAggregate>()
            .AddTransient<IMessageEventService, MessageEventService>()
            .BuildServiceProvider();
    }

    public async ValueTask InitializeAsync() => await _serviceProvider.GetRequiredService<CampaignsDbContext>().Database.EnsureCreatedAsync();

    public async ValueTask DisposeAsync() {
        await _serviceProvider.GetRequiredService<CampaignsDbContext>().Database.EnsureDeletedAsync();
        await _serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task GetDeliveryMetrics_SplitsDispatchedAndFailedPerDay() {
        var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        await SeedAsync(
            Event("Sent", "Email", true, today.AddHours(1)),
            Event("Sent", "Email", true, today.AddHours(2)),
            Event("Sent", "SMS", false, today.AddHours(3)),
            Event("Sent", "SMS", true, today.AddDays(-2).AddHours(5)),
            Event("Created", "Inbox", true, today.AddDays(-2).AddHours(6)),
            // not dispatch attempts
            Event("Created", "Email", true, today.AddHours(1)),
            Event("Read", "Inbox", true, today.AddHours(1)),
            // previous period
            Event("Sent", "Email", false, today.AddDays(-8)),
            Event("Sent", "Email", true, today.AddDays(-9)),
            // outside both periods
            Event("Sent", "Email", true, today.AddDays(-30)));

        var metrics = await _serviceProvider.GetRequiredService<IMessageEventService>()
            .GetDeliveryMetricsAsync(new DeliveryMetricsFilter { TimeFrame = SeriesTimeFrame.Last7Days });

        Assert.Equal(DeliveryInterval.Day, metrics.Interval);
        Assert.Equal(7, metrics.Items.Count);
        Assert.Equal(today.AddDays(-6), metrics.Items[0].Date);
        Assert.Equal(today, metrics.Items[^1].Date);
        Assert.Equal(2, metrics.Items[^1].Dispatched);
        Assert.Equal(1, metrics.Items[^1].Failed);
        Assert.Equal(2, metrics.Items[^3].Dispatched);
        Assert.Equal(0, metrics.Items[^3].Failed);
        Assert.Equal(4, metrics.Current.Dispatched);
        Assert.Equal(1, metrics.Current.Failed);
        Assert.Equal(0.8, metrics.Current.SuccessRate, precision: 3);
        Assert.Equal(1, metrics.Previous.Dispatched);
        Assert.Equal(1, metrics.Previous.Failed);

        var sms = Assert.Single(metrics.PerChannel, x => x.Kind == MessageChannelKind.SMS);
        Assert.Equal(1, sms.Dispatched);
        Assert.Equal(1, sms.Failed);
        Assert.Equal(2, Assert.Single(metrics.PerChannel, x => x.Kind == MessageChannelKind.Email).Dispatched);
        Assert.Equal(1, Assert.Single(metrics.PerChannel, x => x.Kind == MessageChannelKind.Inbox).Dispatched);
    }

    [Fact]
    public async Task GetDeliveryMetrics_FiltersByChannel_AndBucketsPerMonth() {
        var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var thisMonth = new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, TimeSpan.Zero);
        await SeedAsync(
            Event("Sent", "SMS", false, thisMonth.AddHours(1)),
            Event("Sent", "SMS", true, thisMonth.AddMonths(-3).AddDays(2)),
            Event("Sent", "Email", true, thisMonth.AddHours(1)));

        var metrics = await _serviceProvider.GetRequiredService<IMessageEventService>()
            .GetDeliveryMetricsAsync(new DeliveryMetricsFilter { TimeFrame = SeriesTimeFrame.Last12Months, Channel = MessageChannelKind.SMS });

        Assert.Equal(DeliveryInterval.Month, metrics.Interval);
        Assert.Equal(12, metrics.Items.Count);
        Assert.Equal(1, metrics.Items[^1].Failed);
        Assert.Equal(0, metrics.Items[^1].Dispatched);
        Assert.Equal(1, metrics.Items[^4].Dispatched);
        Assert.Equal(1, metrics.Current.Dispatched);
        Assert.Equal(MessageChannelKind.SMS, Assert.Single(metrics.PerChannel).Kind);
    }

    private async Task SeedAsync(params DbMessageEvent[] events) {
        var db = _serviceProvider.GetRequiredService<CampaignsDbContext>();
        db.MessageEvents.AddRange(events);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static DbMessageEvent Event(string type, string channel, bool success, DateTimeOffset createdOn) => new() {
        CampaignId = Guid.NewGuid(),
        ContactId = Guid.NewGuid(),
        Type = type,
        Channel = channel,
        Recipient = "recipient",
        Success = success,
        CreatedOn = createdOn
    };
}
