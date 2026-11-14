using Indice.Hosting.Data;
using Indice.Hosting.Data.Models;
using Indice.Hosting.Diagnostics;
using Indice.Hosting.Services;
using Indice.Hosting.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Indice.Hosting.Tests;

public class WorkerServerRegistryIntegrationTests : IAsyncLifetime
{
    private const string QueueName = "server-registry-queue";
    private readonly string _connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=WorkerDb.Servers.Test_{Environment.Version.Major}_{Guid.NewGuid():N};Trusted_Connection=True;MultipleActiveResultSets=true";
    private readonly ServiceProvider _services;
    private readonly WorkerMetrics _metrics = new();
    private readonly WorkerServerRegistry _registry;

    public WorkerServerRegistryIntegrationTests() {
        _services = new ServiceCollection()
            .AddDbContext<TaskDbContext>(options => options.UseSqlServer(_connectionString))
            .AddScoped<ITaskDbContext>(serviceProvider => serviceProvider.GetRequiredService<TaskDbContext>())
            .BuildServiceProvider();
        _registry = CreateRegistry(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task RegisterAddsTheServerWithItsQueuesAndScheduledTasks() {
        await _registry.Register(TestContext.Current.CancellationToken);

        var server = Assert.Single(await GetServers());
        Assert.Equal(_registry.ServerId, server.Id);
        Assert.Equal(Environment.MachineName, server.MachineName);
        Assert.Equal(Environment.ProcessId, server.ProcessId);
        Assert.Equal(30, server.HeartbeatInterval);
        var queue = Assert.Single(server.GetQueues());
        Assert.Equal(QueueName, queue.Name);
        Assert.Equal(typeof(TestHandler).ToString(), queue.HandlerType);
        Assert.Equal(typeof(TestWorkItem).ToString(), queue.WorkItemType);
        Assert.Equal(500, queue.PollingInterval);
        Assert.Equal(2, queue.InstanceCount);
        Assert.Equal(7, queue.MaxRetryCount);
        var task = Assert.Single(server.GetScheduledTasks());
        Assert.Equal("reports.nightly", task.Id);
        Assert.Equal("0 0 2 * * ?", task.CronExpression);
        Assert.True(task.Singleton);
    }

    [Fact]
    public async Task HeartbeatRefreshesTheServer() {
        await _registry.Register(TestContext.Current.CancellationToken);
        var past = DateTime.UtcNow.AddMinutes(-1);
        await UseContext(db => db.Servers.ExecuteUpdateAsync(s => s.SetProperty(x => x.LastHeartbeat, past), TestContext.Current.CancellationToken));

        await _registry.Heartbeat(TestContext.Current.CancellationToken);

        var server = Assert.Single(await GetServers());
        Assert.True(server.LastHeartbeat > past.AddSeconds(30));
    }

    [Fact]
    public async Task HeartbeatStoresTheMetricsOfTheServer() {
        await _registry.Register(TestContext.Current.CancellationToken);
        Assert.Empty(Assert.Single(await GetServers()).GetMetrics()!.Queues);
        _metrics.MessageCompleted(QueueName, _metrics.MessageStarted(QueueName, DateTime.UtcNow), MessageOutcome.Succeeded);
        _metrics.MessageCompleted(QueueName, _metrics.MessageStarted(QueueName, DateTime.UtcNow), MessageOutcome.Retried);
        _metrics.TaskCompleted("reports.nightly", _metrics.TaskStarted("reports.nightly"), failed: false);

        await _registry.Heartbeat(TestContext.Current.CancellationToken);

        var metrics = Assert.Single(await GetServers()).GetMetrics();
        Assert.NotNull(metrics);
        var queue = Assert.Single(metrics.Queues);
        Assert.Equal(QueueName, queue.Name);
        Assert.Equal(1, queue.Succeeded);
        Assert.Equal(1, queue.Retried);
        Assert.Equal(2, queue.RecentProcessed);
        Assert.Equal(1, queue.RecentFailed);
        Assert.Equal(0, queue.Processing);
        var task = Assert.Single(metrics.Tasks);
        Assert.Equal("reports.nightly", task.Id);
        Assert.Equal(1, task.Succeeded);
        Assert.NotNull(task.LastDuration);
    }

    [Fact]
    public async Task HeartbeatRegistersAgainWhenTheRowIsMissing() {
        await _registry.Register(TestContext.Current.CancellationToken);
        await UseContext(db => db.Servers.ExecuteDeleteAsync(TestContext.Current.CancellationToken));

        await _registry.Heartbeat(TestContext.Current.CancellationToken);

        var server = Assert.Single(await GetServers());
        Assert.Equal(_registry.ServerId, server.Id);
        Assert.Single(server.GetQueues());
    }

    [Fact]
    public async Task HeartbeatRemovesTheServersThatTimedOut() {
        var now = DateTime.UtcNow;
        await UseContext(db => {
            db.Add(NewServer("crashed", now.AddMinutes(-10)));
            db.Add(NewServer("alive", now.AddSeconds(-20)));
            return db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        await _registry.Register(TestContext.Current.CancellationToken);
        await _registry.Heartbeat(TestContext.Current.CancellationToken);

        var ids = (await GetServers()).Select(x => x.Id).Order().ToList();
        Assert.Equal(new[] { "alive", _registry.ServerId }.Order(), ids);
    }

    [Fact]
    public async Task PruningRespectsEachServersHeartbeatInterval() {
        var now = DateTime.UtcNow;
        await UseContext(db => {
            db.Add(NewServer("slow-heartbeat", now.AddMinutes(-6), heartbeatInterval: 600));
            db.Add(NewServer("timed-out", now.AddMinutes(-6)));
            return db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        await _registry.Register(TestContext.Current.CancellationToken);

        var ids = (await GetServers()).Select(x => x.Id).ToList();
        Assert.Contains("slow-heartbeat", ids);
        Assert.DoesNotContain("timed-out", ids);
    }

    [Fact]
    public async Task UnregisterRemovesTheServer() {
        await UseContext(db => {
            db.Add(NewServer("other", DateTime.UtcNow));
            return db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });
        await _registry.Register(TestContext.Current.CancellationToken);

        await _registry.Unregister();

        var server = Assert.Single(await GetServers());
        Assert.Equal("other", server.Id);
    }

    [Fact]
    public async Task RegistrationDoesNotThrowWhenTheTableIsMissing() {
        await UseContext(db => db.Database.ExecuteSqlRawAsync("DROP TABLE [work].[Server]", TestContext.Current.CancellationToken));

        await _registry.Register(TestContext.Current.CancellationToken);
        await _registry.Heartbeat(TestContext.Current.CancellationToken);
        await _registry.Unregister();

        // Once the table exists, the next heartbeat registers the server.
        await UseContext(db => db.Database.ExecuteSqlRawAsync(
            "CREATE TABLE [work].[Server] ([Id] nvarchar(256) NOT NULL PRIMARY KEY, [MachineName] nvarchar(128) NOT NULL, [ProcessId] int NOT NULL, " +
            "[ApplicationName] nvarchar(256) NULL, [Version] nvarchar(128) NULL, [StartedAt] datetime2 NOT NULL, [LastHeartbeat] datetime2 NOT NULL, " +
            "[HeartbeatInterval] int NOT NULL, [Queues] nvarchar(max) NULL, [ScheduledTasks] nvarchar(max) NULL, [Metrics] nvarchar(max) NULL)",
            TestContext.Current.CancellationToken));
        await _registry.Heartbeat(TestContext.Current.CancellationToken);

        var server = Assert.Single(await GetServers());
        Assert.Equal(_registry.ServerId, server.Id);
    }

    public async ValueTask InitializeAsync() {
        await UseContext(db => db.Database.EnsureCreatedAsync());
    }

    public async ValueTask DisposeAsync() {
        await UseContext(db => db.Database.EnsureDeletedAsync());
        await _services.DisposeAsync();
        _metrics.Dispose();
    }

    private WorkerServerRegistry CreateRegistry(TimeSpan timeout) => new(
        _services.GetRequiredService<IServiceScopeFactory>(),
        new ServerRegistrationSettings(TimeSpan.FromSeconds(30), timeout),
        [new DequeueJobSettings(typeof(TestHandler), typeof(TestWorkItem), QueueName, 500, 5000, 0, 1000, 2, 7)],
        [new ScheduledJobSettings(typeof(TestHandler), typeof(TestWorkItem), "0 0 2 * * ?", "nightly", "reports", "Nightly report", singleton: true)],
        NullLogger<WorkerServerRegistry>.Instance,
        metrics: _metrics
    );

    private static DbServer NewServer(string id, DateTime lastHeartbeat, int heartbeatInterval = 30) => new() {
        Id = id,
        MachineName = "OTHER-MACHINE",
        ProcessId = 1,
        StartedAt = lastHeartbeat,
        LastHeartbeat = lastHeartbeat,
        HeartbeatInterval = heartbeatInterval
    };

    private async Task<List<DbServer>> GetServers() {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<TaskDbContext>().Servers.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
    }

    private async Task UseContext<T>(Func<TaskDbContext, Task<T>> action) {
        await using var scope = _services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<TaskDbContext>());
    }

    private sealed class TestWorkItem;

    private sealed class TestHandler;
}
