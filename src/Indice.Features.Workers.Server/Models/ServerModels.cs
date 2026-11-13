namespace Indice.Features.Workers.Server.Models;

/// <summary>A distributed lock.</summary>
public class LockInfo
{
    /// <summary>The lock id.</summary>
    public Guid Id { get; set; }
    /// <summary>The subject of the lock.</summary>
    public string Name { get; set; } = null!;
    /// <summary>The lease expiration date, in UTC.</summary>
    public DateTime ExpirationDate { get; set; }
    /// <summary>The lease duration in seconds.</summary>
    public int DurationSeconds { get; set; }
    /// <summary>Whether the lease has expired.</summary>
    public bool IsExpired { get; set; }
}

/// <summary>A worker process (a node of the cluster).</summary>
/// <remarks>
/// Workers register themselves in the server table on startup, with the queues they consume and the scheduled tasks they host.
/// Workers that are not registered (<see cref="WorkerServerStatus.Unknown"/>) are only seen from the scheduled tasks they have run.
/// </remarks>
public class WorkerServerInfo
{
    /// <summary>The server id, <c>{machine}:{process}:{random}</c>, or the machine name when the worker is not registered.</summary>
    public string WorkerId { get; set; } = null!;
    /// <summary>The machine name. Matches <see cref="ScheduledTaskInfo.WorkerId"/>.</summary>
    public string MachineName { get; set; } = null!;
    /// <summary>The status of the worker.</summary>
    public WorkerServerStatus Status { get; set; }
    /// <summary>The process id. Only known for a registered worker.</summary>
    public int? ProcessId { get; set; }
    /// <summary>The application name of the host. Only known for a registered worker.</summary>
    public string? ApplicationName { get; set; }
    /// <summary>The version of the application. Only known for a registered worker.</summary>
    public string? Version { get; set; }
    /// <summary>When the worker started. Only known for a registered worker.</summary>
    public DateTimeOffset? StartedAt { get; set; }
    /// <summary>The last heartbeat of a registered worker, otherwise the latest execution of its tasks.</summary>
    public DateTimeOffset? LastSeen { get; set; }
    /// <summary>The heartbeat interval in seconds. Only known for a registered worker.</summary>
    public int? HeartbeatInterval { get; set; }
    /// <summary>The queues the worker consumes. Empty when the worker is not registered.</summary>
    public List<string> Queues { get; set; } = [];
    /// <summary>The ids of the scheduled tasks the worker hosts. Empty when the worker is not registered.</summary>
    public List<string> ScheduledTasks { get; set; } = [];
    /// <summary>The tasks whose last run was on the machine of this worker.</summary>
    public int Tasks { get; set; }
    /// <summary>The tasks currently running on the machine of this worker.</summary>
    public int RunningTasks { get; set; }
}
