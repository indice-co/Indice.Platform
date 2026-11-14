using System.Text.Json;

namespace Indice.Hosting.Data.Models;

/// <summary>A worker process (a node of the cluster) that hosts the background tasks. It registers itself on startup and keeps its row alive with a heartbeat.</summary>
public class DbServer
{
    /// <summary>The server id, <c>{MachineName}:{ProcessId}:{random}</c>. Unique per process, so a restart registers a new server.</summary>
    public string Id { get; set; } = null!;
    /// <summary>The machine name. Matches <see cref="DbScheduledTask.WorkerId"/>.</summary>
    public string MachineName { get; set; } = null!;
    /// <summary>The process id.</summary>
    public int ProcessId { get; set; }
    /// <summary>The application name of the host.</summary>
    public string? ApplicationName { get; set; }
    /// <summary>The version of the application.</summary>
    public string? Version { get; set; }
    /// <summary>When the server started, in UTC.</summary>
    public DateTime StartedAt { get; set; }
    /// <summary>The last heartbeat of the server, in UTC.</summary>
    public DateTime LastHeartbeat { get; set; }
    /// <summary>The heartbeat interval of the server in seconds. A server that misses a few heartbeats is stale.</summary>
    public int HeartbeatInterval { get; set; }
    /// <summary>The queues the server consumes with their settings, as a JSON array of <see cref="DbServerQueue"/>.</summary>
    public string? Queues { get; set; }
    /// <summary>The scheduled tasks the server hosts, as a JSON array of <see cref="DbServerScheduledTask"/>.</summary>
    public string? ScheduledTasks { get; set; }
    /// <summary>The work of the server measured in process, as a JSON <see cref="DbServerMetrics"/>. Refreshed on every heartbeat.</summary>
    public string? Metrics { get; set; }

    /// <summary>Reads <see cref="Queues"/>.</summary>
    public IReadOnlyList<DbServerQueue> GetQueues() => Deserialize<List<DbServerQueue>>(Queues) ?? [];

    /// <summary>Reads <see cref="ScheduledTasks"/>.</summary>
    public IReadOnlyList<DbServerScheduledTask> GetScheduledTasks() => Deserialize<List<DbServerScheduledTask>>(ScheduledTasks) ?? [];

    /// <summary>Reads <see cref="Metrics"/>. Null when the server does not report metrics.</summary>
    public DbServerMetrics? GetMetrics() => Deserialize<DbServerMetrics>(Metrics);

    internal static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private static T? Deserialize<T>(string? json) where T : class {
        if (string.IsNullOrWhiteSpace(json)) {
            return null;
        }
        try {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        } catch (JsonException) {
            return null;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}

/// <summary>A queue consumed by a <see cref="DbServer"/>, with the settings of that server.</summary>
public class DbServerQueue
{
    /// <summary>The queue name.</summary>
    public string Name { get; set; } = null!;
    /// <summary>The type of the work item.</summary>
    public string? WorkItemType { get; set; }
    /// <summary>The type of the handler.</summary>
    public string? HandlerType { get; set; }
    /// <summary>The time interval between two attempts to dequeue new items, in milliseconds.</summary>
    public double PollingInterval { get; set; }
    /// <summary>The maximum time interval between two attempts to dequeue new items, in milliseconds.</summary>
    public double MaxPollingInterval { get; set; }
    /// <summary>The number of concurrent dequeue instances of the server.</summary>
    public int InstanceCount { get; set; }
    /// <summary>The maximum number of attempts before a message becomes poison.</summary>
    public int MaxRetryCount { get; set; }
    /// <summary>The cleanup interval in seconds. Zero or less when cleanup is off.</summary>
    public int CleanupInterval { get; set; }
    /// <summary>The cleanup batch size.</summary>
    public int CleanupBatchSize { get; set; }
}

/// <summary>A scheduled task hosted by a <see cref="DbServer"/>.</summary>
public class DbServerScheduledTask
{
    /// <summary>The task id, i.e. the Quartz job key (<c>{group}.{name}</c>). Matches <see cref="DbScheduledTask.Id"/>.</summary>
    public string Id { get; set; } = null!;
    /// <summary>The task group.</summary>
    public string? Group { get; set; }
    /// <summary>The task description.</summary>
    public string? Description { get; set; }
    /// <summary>The handler type.</summary>
    public string? HandlerType { get; set; }
    /// <summary>The cron expression.</summary>
    public string? CronExpression { get; set; }
    /// <summary>Whether only one worker may run the task at a time.</summary>
    public bool Singleton { get; set; }
}

/// <summary>The work of a <see cref="DbServer"/>, measured in process: totals since the server started and the activity of the last <see cref="Window"/>.</summary>
public class DbServerMetrics
{
    /// <summary>When the metrics were collected, in UTC.</summary>
    public DateTime CollectedAt { get; set; }
    /// <summary>The period of the recent activity, in seconds.</summary>
    public double Window { get; set; }
    /// <summary>The queues that processed messages since the server started. A queue the server consumes but is missing here processed none.</summary>
    public List<DbServerQueueMetrics> Queues { get; set; } = [];
    /// <summary>The scheduled tasks that ran since the server started.</summary>
    public List<DbServerTaskMetrics> Tasks { get; set; } = [];
}

/// <summary>The work of a <see cref="DbServer"/> on a queue.</summary>
public class DbServerQueueMetrics
{
    /// <summary>The queue name.</summary>
    public string Name { get; set; } = null!;
    /// <summary>The messages being processed now.</summary>
    public long Processing { get; set; }
    /// <summary>The messages processed successfully since the server started.</summary>
    public long Succeeded { get; set; }
    /// <summary>The messages that failed and went back to the queue since the server started.</summary>
    public long Retried { get; set; }
    /// <summary>The messages that failed for the last time and became poison since the server started.</summary>
    public long Poisoned { get; set; }
    /// <summary>The messages processed in the last <see cref="DbServerMetrics.Window"/>, successfully or not.</summary>
    public long RecentProcessed { get; set; }
    /// <summary>The messages dequeued in the last <see cref="DbServerMetrics.Window"/>. Null for older workers that do not report this count.</summary>
    public long? RecentStarted { get; set; }
    /// <summary>The messages that failed in the last <see cref="DbServerMetrics.Window"/>.</summary>
    public long RecentFailed { get; set; }
    /// <summary>The average handler time in the last <see cref="DbServerMetrics.Window"/>, in milliseconds.</summary>
    public double? RecentAverageDuration { get; set; }
    /// <summary>The average time the messages dequeued in the last <see cref="DbServerMetrics.Window"/> waited in the queue, in milliseconds.</summary>
    public double? RecentAverageWait { get; set; }
}

/// <summary>The work of a <see cref="DbServer"/> on a scheduled task.</summary>
public class DbServerTaskMetrics
{
    /// <summary>The task id. Matches <see cref="DbServerScheduledTask.Id"/>.</summary>
    public string Id { get; set; } = null!;
    /// <summary>The runs in progress.</summary>
    public long Running { get; set; }
    /// <summary>The runs that succeeded since the server started.</summary>
    public long Succeeded { get; set; }
    /// <summary>The runs that failed since the server started.</summary>
    public long Failed { get; set; }
    /// <summary>The average run time since the server started, in milliseconds.</summary>
    public double? AverageDuration { get; set; }
    /// <summary>The time the last run took, in milliseconds.</summary>
    public double? LastDuration { get; set; }
    /// <summary>When the last run completed, in UTC. Null for older workers that do not report this timestamp.</summary>
    public DateTime? LastCompletedAt { get; set; }
}
