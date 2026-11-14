using System.Reflection;
using Indice.Hosting.Data;
using Indice.Hosting.Data.Models;
using Indice.Hosting.Diagnostics;
using Indice.Hosting.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Indice.Hosting.Services;

/// <summary>Settings of the server registration.</summary>
/// <param name="HeartbeatInterval">How often the server refreshes its row.</param>
/// <param name="Timeout">The minimum age of a server's last heartbeat before other servers consider removing its row.</param>
internal sealed record ServerRegistrationSettings(TimeSpan HeartbeatInterval, TimeSpan Timeout);

/// <summary>
/// Registers the current worker process in the <c>work.Server</c> table, with the queues it consumes and the scheduled tasks it hosts,
/// so that the workers dashboard can show the live servers, the configuration of the queues and the work of each server.
/// </summary>
/// <remarks>
/// A worker that stops gracefully removes its row. A worker that crashes cannot, so every heartbeat also removes servers whose last heartbeat is older than the local timeout and at least three of their advertised heartbeat intervals.
/// Failures are logged and never stop the worker: e.g. when the table is missing because the database was not migrated, the next heartbeat simply tries again.
/// </remarks>
internal sealed class WorkerServerRegistry
{
    private const int MaxVersionLength = 128;
    private const int MissedHeartbeats = 3;
    private static readonly TimeSpan UnregisterTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MinMetricsWindow = TimeSpan.FromMinutes(1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ServerRegistrationSettings _settings;
    private readonly ILogger<WorkerServerRegistry> _logger;
    private readonly WorkerMetrics? _metrics;
    private readonly int _queueCount;
    private readonly string _queues;
    private readonly string _scheduledTasks;
    private readonly string? _applicationName;
    private readonly string? _version;
    private volatile bool _registered;
    private volatile bool _failing;

    public WorkerServerRegistry(
        IServiceScopeFactory scopeFactory,
        ServerRegistrationSettings settings,
        IEnumerable<DequeueJobSettings> dequeueJobSettings,
        IEnumerable<ScheduledJobSettings> scheduledJobSettings,
        ILogger<WorkerServerRegistry> logger,
        IHostEnvironment? hostEnvironment = null,
        WorkerMetrics? metrics = null
    ) {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = metrics;
        ArgumentNullException.ThrowIfNull(dequeueJobSettings);
        ArgumentNullException.ThrowIfNull(scheduledJobSettings);
        ServerId = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid().ToString("N")[..8]}";
        StartedAt = DateTime.UtcNow;
        _applicationName = hostEnvironment?.ApplicationName;
        _version = GetVersion();
        // The same queue configured twice: the last configuration wins, as in the DI container.
        var queues = dequeueJobSettings
            .GroupBy(x => x.Name, StringComparer.Ordinal)
            .Select(group => group.Last())
            .Select(x => new DbServerQueue {
                Name = x.Name,
                WorkItemType = x.WorkItemType.ToString(),
                HandlerType = x.JobHandlerType.ToString(),
                PollingInterval = x.PollingInterval,
                MaxPollingInterval = x.MaxPollingInterval,
                InstanceCount = x.InstanceCount,
                MaxRetryCount = x.MaxRetryCount,
                CleanupInterval = x.CleanupInterval,
                CleanupBatchSize = x.CleanupBatchSize
            })
            .ToList();
        _queueCount = queues.Count;
        _queues = DbServer.Serialize(queues);
        _scheduledTasks = DbServer.Serialize(scheduledJobSettings.Select(x => {
            var group = x.Group ?? JobGroups.InternalJobsGroup;
            return new DbServerScheduledTask {
                Id = new JobKey(x.Name, group).ToString(),
                Group = group,
                Description = x.Description,
                HandlerType = x.JobHandlerType.ToString(),
                CronExpression = x.CronExpression,
                Singleton = x.Singleton
            };
        }));
    }

    /// <summary>The id of the current server.</summary>
    public string ServerId { get; }
    /// <summary>When the current server started, in UTC.</summary>
    public DateTime StartedAt { get; }
    /// <summary>The heartbeat interval.</summary>
    public TimeSpan HeartbeatInterval => _settings.HeartbeatInterval;

    /// <summary>Adds the server row. Called on startup, and by a heartbeat that finds the row missing.</summary>
    public async Task Register(CancellationToken cancellationToken = default) {
        try {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITaskDbContext>();
            var now = DateTime.UtcNow;
            await UpsertServer(dbContext, now, cancellationToken);
            await PruneStaleServers(dbContext, now, cancellationToken);
            _registered = true;
            _failing = false;
            _logger.LogInformation("Worker server '{ServerId}' registered with {QueueCount} queue(s).", ServerId, _queueCount);
        } catch (Exception exception) when (!cancellationToken.IsCancellationRequested) {
            OnFailure(exception);
        }
    }

    /// <summary>Refreshes the heartbeat of the server and removes the servers that stopped sending theirs.</summary>
    public async Task Heartbeat(CancellationToken cancellationToken = default) {
        if (!_registered) {
            await Register(cancellationToken);
            return;
        }
        try {
            await using (var scope = _scopeFactory.CreateAsyncScope()) {
                var dbContext = scope.ServiceProvider.GetRequiredService<ITaskDbContext>();
                var now = DateTime.UtcNow;
                var metrics = CollectMetrics();
                var updated = await dbContext.Servers
                    .Where(x => x.Id == ServerId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.LastHeartbeat, now)
                        .SetProperty(x => x.Metrics, metrics), cancellationToken);
                if (updated > 0) {
                    await PruneStaleServers(dbContext, now, cancellationToken);
                    _failing = false;
                    return;
                }
            }
            // Another server removed the row, e.g. after a long pause or a database outage, so register again.
            _logger.LogWarning("Worker server '{ServerId}' was missing from the server table and registers again.", ServerId);
            _registered = false;
        } catch (Exception exception) when (!cancellationToken.IsCancellationRequested) {
            OnFailure(exception);
            return;
        }
        await Register(cancellationToken);
    }

    /// <summary>Removes the server row. Best effort: a server that fails to do so is removed by the others when it times out.</summary>
    public async Task Unregister() {
        using var timeout = new CancellationTokenSource(UnregisterTimeout);
        try {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITaskDbContext>();
            await dbContext.Servers.Where(x => x.Id == ServerId).ExecuteDeleteAsync(timeout.Token);
            _registered = false;
            _logger.LogInformation("Worker server '{ServerId}' unregistered.", ServerId);
        } catch (Exception exception) {
            _logger.LogWarning(exception, "Worker server '{ServerId}' could not unregister. Its row is removed by the other servers after {Timeout}.", ServerId, _settings.Timeout);
        }
    }

    private async Task UpsertServer(ITaskDbContext dbContext, DateTime now, CancellationToken cancellationToken) {
        var heartbeatInterval = (int)Math.Ceiling(_settings.HeartbeatInterval.TotalSeconds);
        var metrics = CollectMetrics();
        var updated = await dbContext.Servers
            .Where(x => x.Id == ServerId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.LastHeartbeat, now)
                .SetProperty(x => x.HeartbeatInterval, heartbeatInterval)
                .SetProperty(x => x.Queues, _queues)
                .SetProperty(x => x.ScheduledTasks, _scheduledTasks)
                .SetProperty(x => x.Metrics, metrics), cancellationToken);
        if (updated > 0) {
            return;
        }
        // The id is unique per process, so no other server inserts it concurrently.
        var entry = dbContext.Add(new DbServer {
            Id = ServerId,
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            ApplicationName = _applicationName,
            Version = _version,
            StartedAt = StartedAt,
            LastHeartbeat = now,
            HeartbeatInterval = heartbeatInterval,
            Queues = _queues,
            ScheduledTasks = _scheduledTasks,
            Metrics = metrics
        });
        try {
            await dbContext.SaveChangesAsync(cancellationToken);
        } finally {
            entry.State = EntityState.Detached;
        }
    }

    private async Task<int> PruneStaleServers(ITaskDbContext dbContext, DateTime now, CancellationToken cancellationToken) {
        var staleBefore = now - _settings.Timeout;
        // An index seek on LastHeartbeat that usually deletes nothing.
        var candidates = await dbContext.Servers
            .Where(x => x.LastHeartbeat < staleBefore && x.Id != ServerId)
            .Select(x => new { x.Id, x.LastHeartbeat, x.HeartbeatInterval })
            .ToListAsync(cancellationToken);
        var staleIds = candidates
            .Where(x => now - x.LastHeartbeat >= TimeSpan.FromSeconds(Math.Max((long)x.HeartbeatInterval, 1) * MissedHeartbeats))
            .Select(x => x.Id)
            .ToArray();
        return staleIds.Length == 0
            ? 0
            : await dbContext.Servers
                .Where(x => staleIds.Contains(x.Id) && x.LastHeartbeat < staleBefore)
                .ExecuteDeleteAsync(cancellationToken);
    }

    private string? CollectMetrics() {
        if (_metrics is null) {
            return null;
        }
        // The recent activity spans at least a minute, so that rates per minute are steady with a short heartbeat.
        var window = _settings.HeartbeatInterval > MinMetricsWindow ? _settings.HeartbeatInterval : MinMetricsWindow;
        return DbServer.Serialize(_metrics.Collect(window));
    }

    private void OnFailure(Exception exception) {
        // Warn once per outage, so a database without the table does not flood the logs every heartbeat.
        if (_failing) {
            _logger.LogDebug(exception, "Worker server '{ServerId}' could not update the server registration.", ServerId);
            return;
        }
        _failing = true;
        _logger.LogWarning(
            exception,
            "Worker server '{ServerId}' could not update the server registration; it retries every {HeartbeatInterval}. " +
            "The workers keep running, but the dashboard cannot list this server. Make sure the database has the [work].[Server] table.",
            ServerId,
            _settings.HeartbeatInterval
        );
    }

    private static string? GetVersion() {
        var assembly = Assembly.GetEntryAssembly();
        var version = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? assembly?.GetName().Version?.ToString();
        return version?.Length > MaxVersionLength ? version[..MaxVersionLength] : version;
    }
}
