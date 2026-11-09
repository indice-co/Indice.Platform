using HandlebarsDotNet;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Rendering;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Indice.Features.Messages.Tests;

public class ChannelMetricsTests : IAsyncLifetime
{
    private readonly ServiceProvider _serviceProvider;

    public ChannelMetricsTests() {
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=MessagesDb.ChannelMetricsTest_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true";
        _serviceProvider = new ServiceCollection()
            .AddDbContext<CampaignsDbContext>(builder => builder.UseSqlServer(connectionString))
            .AddTransient(serviceProvider => new DatabaseSchemaNameResolver("cmp"))
            .AddTransient<IUserNameAccessor, UserNameAccessorNoOp>()
            .AddTransient<UserNameAccessorAggregate>()
            .BuildServiceProvider();
    }

    public async ValueTask InitializeAsync() => await _serviceProvider.GetRequiredService<CampaignsDbContext>().Database.EnsureCreatedAsync();

    public async ValueTask DisposeAsync() {
        await _serviceProvider.GetRequiredService<CampaignsDbContext>().Database.EnsureDeletedAsync();
        await _serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task GetChannelMetrics_CountsEachMessageOncePerChannel() {
        var campaignId = Guid.NewGuid();
        var retried = Guid.NewGuid();
        var recovered = Guid.NewGuid();
        var delivered = Guid.NewGuid();
        await SeedAsync([
            // 7 failed attempts of the same email: one failure
            ..Enumerable.Range(0, 7).Select(_ => Event(campaignId, retried, "Sent", "Email", false)),
            // failed, then delivered on retry: not a failure
            Event(campaignId, recovered, "Sent", "Email", false),
            Event(campaignId, recovered, "Sent", "Email", true),
            Event(campaignId, delivered, "Sent", "Email", true),
            Event(campaignId, delivered, "Sent", "SMS", true),
            Event(campaignId, delivered, "Created", "Inbox", true),
            // not dispatch attempts
            Event(campaignId, delivered, "Created", "Email", true),
            Event(campaignId, delivered, "Read", "Inbox", true),
            // another campaign
            Event(Guid.NewGuid(), Guid.NewGuid(), "Sent", "Email", false)
        ]);

        var metrics = await CreateService().GetChannelMetrics(campaignId);

        var email = Assert.Single(metrics, x => x.Kind == MessageChannelKind.Email);
        Assert.Equal(3, email.Total);
        Assert.Equal(1, email.Failures);
        var sms = Assert.Single(metrics, x => x.Kind == MessageChannelKind.SMS);
        Assert.Equal(1, sms.Total);
        Assert.Equal(0, sms.Failures);
        Assert.Equal(1, Assert.Single(metrics, x => x.Kind == MessageChannelKind.Inbox).Total);
        Assert.Equal(3, metrics.Count);
    }

    private CampaignService CreateService() => new(
        _serviceProvider.GetRequiredService<CampaignsDbContext>(),
        Options.Create(new MessageManagementOptions()),
        new NoPartialTemplateResolverFactory());

    private async Task SeedAsync(DbMessageEvent[] events) {
        var db = _serviceProvider.GetRequiredService<CampaignsDbContext>();
        db.MessageEvents.AddRange(events);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static DbMessageEvent Event(Guid campaignId, Guid messageId, string type, string channel, bool success) => new() {
        CampaignId = campaignId,
        ContactId = messageId,
        MessageId = messageId,
        Type = type,
        Channel = channel,
        Recipient = "recipient",
        Success = success
    };

    private sealed class NoPartialTemplateResolverFactory : IPartialTemplateResolverFactory
    {
        public IPartialTemplateResolver Create(string channel) => throw new NotSupportedException();
    }
}
