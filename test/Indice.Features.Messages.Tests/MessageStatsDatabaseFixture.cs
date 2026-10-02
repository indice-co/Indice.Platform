using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Indice.Features.Messages.Tests;

[CollectionDefinition(Name)]
public class MessageStatsCollection : ICollectionFixture<MessageStatsDatabaseFixture>
{
    public const string Name = "MessageStats";
}

/// <summary>One database for all the send statistics tests. Every test starts with empty tables, see <see cref="ResetAsync"/>.</summary>
public class MessageStatsDatabaseFixture : IAsyncLifetime
{
    public const int MaxCarriedFlushes = 3;

    public MessageStatsDatabaseFixture() {
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=MessagesDb.Test_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true";
        var services = new ServiceCollection()
            .AddLogging()
            .AddDbContext<CampaignsDbContext>(builder => builder.UseSqlServer(connectionString))
            .AddTransient(serviceProvider => new DatabaseSchemaNameResolver("cmp"))
            .AddTransient<IUserNameAccessor, UserNameAccessorNoOp>()
            .AddTransient<UserNameAccessorAggregate>()
            .AddSingleton<ILockManager>(LockManager)
            .Configure<AnalyticsOptions>(options => {
                options.Stats.Enabled = true;
                options.Stats.MaxCarriedFlushes = MaxCarriedFlushes;
            });
        ServiceProvider = services.BuildServiceProvider();
    }

    public ServiceProvider ServiceProvider { get; }
    internal TestLockManager LockManager { get; } = new();
    public IOptions<AnalyticsOptions> Options => ServiceProvider.GetRequiredService<IOptions<AnalyticsOptions>>();

    public async ValueTask InitializeAsync() {
        using var scope = ServiceProvider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CampaignsDbContext>().Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync() {
        using (var scope = ServiceProvider.CreateScope()) {
            await scope.ServiceProvider.GetRequiredService<CampaignsDbContext>().Database.EnsureDeletedAsync();
        }
        await ServiceProvider.DisposeAsync();
    }

    public async Task ResetAsync() {
        LockManager.Unavailable = false;
        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CampaignsDbContext>();
        await dbContext.MessageStats.ExecuteDeleteAsync();
        await dbContext.MessageEvents.ExecuteDeleteAsync();
        await dbContext.Campaigns.ExecuteDeleteAsync();
        await dbContext.MessageTypes.ExecuteDeleteAsync();
    }

    // The writer does not wait for a lock that is held, so that a test of a held lock does not take seconds.
    public MessageStatsWriter CreateWriter() => new(ServiceProvider.GetRequiredService<IServiceScopeFactory>(), NullLogger<MessageStatsWriter>.Instance) {
        LockRetryCount = 0
    };

    public MessageStatsHostedService CreateHostedService(MessageStatsQueue? queue = null) => new(
        queue ?? new MessageStatsQueue(Options),
        CreateWriter(),
        ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
        Options,
        NullLogger<MessageStatsHostedService>.Instance
    );

    public async Task<List<DbMessageStat>> GetStatsAsync() {
        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CampaignsDbContext>();
        return await dbContext.MessageStats.AsNoTracking().ToListAsync();
    }

    public async Task<Guid> AddMessageTypeAsync(string name) {
        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CampaignsDbContext>();
        var messageType = new DbMessageType { Id = Guid.NewGuid(), Name = name };
        dbContext.MessageTypes.Add(messageType);
        await dbContext.SaveChangesAsync();
        return messageType.Id;
    }

    public async Task<Guid> AddCampaignAsync(Guid? typeId = null, bool isGlobal = false) {
        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CampaignsDbContext>();
        var campaign = new DbCampaign {
            Id = Guid.NewGuid(),
            Title = $"Test Campaign {Guid.NewGuid()}",
            MessageChannelKind = MessageChannelKind.Email,
            Published = true,
            IsGlobal = isGlobal,
            TypeId = typeId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test",
            Content = new MessageContentDictionary {
                [nameof(MessageChannelKind.Email)] = new MessageContent { Title = "Test", Body = "Test Body" }
            }
        };
        dbContext.Campaigns.Add(campaign);
        await dbContext.SaveChangesAsync();
        return campaign.Id;
    }

    public async Task AddEventsAsync(Guid campaignId, string channel, string type, DateTimeOffset createdOn, int count = 1, bool success = true) {
        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CampaignsDbContext>();
        for (var i = 0; i < count; i++) {
            dbContext.MessageEvents.Add(new DbMessageEvent {
                CampaignId = campaignId,
                ContactId = Guid.NewGuid(),
                Channel = channel,
                Type = type,
                Recipient = "user",
                Success = success,
                CreatedOn = createdOn
            });
        }
        await dbContext.SaveChangesAsync();
    }

    public async Task AddStatAsync(MessageStatGranularity granularity, DateOnly periodStart, MessageChannelKind channel, Guid campaignTypeId, int successfulSends) {
        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CampaignsDbContext>();
        dbContext.MessageStats.Add(new DbMessageStat {
            Granularity = granularity,
            PeriodStart = periodStart,
            Channel = channel,
            CampaignTypeId = campaignTypeId,
            SuccessfulSends = successfulSends
        });
        await dbContext.SaveChangesAsync();
    }
}

/// <summary>A lock inside the test process. It can also behave as held by another process.</summary>
internal class TestLockManager : ILockManager
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>When true the lock cannot be acquired, as if another process holds it.</summary>
    public bool Unavailable { get; set; }
    public bool IsHeld => _semaphore.CurrentCount == 0;

    public async Task<ILockLease> AcquireLock(string name, TimeSpan? duration = null, CancellationToken cancellationToken = default) {
        if (Unavailable) {
            throw new LockManagerException(name);
        }
        await _semaphore.WaitAsync(cancellationToken);
        return new LockLease(Guid.NewGuid().ToString(), name, this);
    }

    public Task ReleaseLock(ILockLease @lock) {
        _semaphore.Release();
        return Task.CompletedTask;
    }

    public Task<ILockLease> Renew(string name, string leaseId, CancellationToken cancellationToken = default) => Task.FromResult<ILockLease>(new LockLease(leaseId, name, this));

    public Task Cleanup() => Task.CompletedTask;
}
