using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Indice.Features.Messages.Tests;

/// <summary>Tests that the event service posts to the send statistics queue only what it saved. The save is intercepted, so no database is used.</summary>
public class MessageStatsEventHookTests
{
    [Fact]
    public async Task SavedBatch_PostsTheSuccessfulSends() {
        var interceptor = new SaveInterceptor(fail: false);
        await using var serviceProvider = BuildServiceProvider(interceptor);
        var service = serviceProvider.GetRequiredService<MessageEventHostedServcie>();
        var statsQueue = serviceProvider.GetRequiredService<MessageStatsQueue>();
        var campaignId = Guid.NewGuid();
        await service.StartAsync(TestContext.Current.CancellationToken);

        await EnqueueAsync(serviceProvider, campaignId, MessageChannelKind.Email, MessageEventType.Sent, success: true);
        await EnqueueAsync(serviceProvider, campaignId, MessageChannelKind.Email, MessageEventType.Sent, success: false);
        await EnqueueAsync(serviceProvider, campaignId, MessageChannelKind.Inbox, MessageEventType.Created, success: true);
        await EnqueueAsync(serviceProvider, campaignId, MessageChannelKind.Inbox, MessageEventType.Read, success: true);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        var deltas = new List<MessageStatDelta> {
            await statsQueue.Reader.ReadAsync(timeout.Token),
            await statsQueue.Reader.ReadAsync(timeout.Token)
        };
        await service.StopAsync(TestContext.Current.CancellationToken);

        Assert.All(deltas, x => Assert.Equal(campaignId, x.CampaignId));
        Assert.Equal([MessageChannelKind.Inbox, MessageChannelKind.Email], deltas.Select(x => x.Channel).Order());
        Assert.False(statsQueue.Reader.TryRead(out _));
    }

    [Fact]
    public async Task FailedSave_PostsNothing() {
        var interceptor = new SaveInterceptor(fail: true);
        await using var serviceProvider = BuildServiceProvider(interceptor);
        var service = serviceProvider.GetRequiredService<MessageEventHostedServcie>();
        var statsQueue = serviceProvider.GetRequiredService<MessageStatsQueue>();
        await service.StartAsync(TestContext.Current.CancellationToken);

        await EnqueueAsync(serviceProvider, Guid.NewGuid(), MessageChannelKind.Email, MessageEventType.Sent, success: true);

        await interceptor.Saving.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        // Stopping waits for the service to finish the batch that failed.
        await service.StopAsync(TestContext.Current.CancellationToken);

        Assert.False(statsQueue.Reader.TryRead(out _));
    }

    private static ValueTask EnqueueAsync(ServiceProvider serviceProvider, Guid campaignId, MessageChannelKind channel, MessageEventType type, bool success) =>
        serviceProvider.GetRequiredService<MessageEventQueue>().EnqueueAsync(new MessageEvent {
            CampaignId = campaignId,
            ContactId = Guid.NewGuid(),
            Channel = channel.ToString(),
            Type = type.ToString(),
            Recipient = "user",
            Success = success
        });

    private static ServiceProvider BuildServiceProvider(SaveInterceptor interceptor) => new ServiceCollection()
        .AddLogging()
        // The connection is never opened, because the interceptor answers the save.
        .AddDbContext<CampaignsDbContext>(builder => builder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=MessagesDb.NotUsed;Trusted_Connection=True").AddInterceptors(interceptor))
        .AddTransient(serviceProvider => new DatabaseSchemaNameResolver("cmp"))
        .AddTransient<IUserNameAccessor, UserNameAccessorNoOp>()
        .AddTransient<UserNameAccessorAggregate>()
        .Configure<AnalyticsOptions>(options => options.Stats.Enabled = true)
        .AddSingleton<MessageEventQueue>()
        .AddSingleton<MessageStatsQueue>()
        .AddSingleton<MessageEventHostedServcie>()
        .BuildServiceProvider();

    private class SaveInterceptor(bool fail) : SaveChangesInterceptor
    {
        public TaskCompletionSource Saving { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) {
            Saving.TrySetResult();
            if (fail) {
                throw new InvalidOperationException("The save failed.");
            }
            return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(eventData.Context!.ChangeTracker.Entries().Count()));
        }
    }
}
