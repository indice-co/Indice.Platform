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

    /// <summary>Reads <see cref="Queues"/>.</summary>
    public IReadOnlyList<DbServerQueue> GetQueues() => Deserialize<DbServerQueue>(Queues);

    /// <summary>Reads <see cref="ScheduledTasks"/>.</summary>
    public IReadOnlyList<DbServerScheduledTask> GetScheduledTasks() => Deserialize<DbServerScheduledTask>(ScheduledTasks);

    internal static string Serialize<T>(IEnumerable<T> items) => JsonSerializer.Serialize(items, JsonOptions);

    private static List<T> Deserialize<T>(string? json) {
        if (string.IsNullOrWhiteSpace(json)) {
            return [];
        }
        try {
            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? [];
        } catch (JsonException) {
            return [];
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
