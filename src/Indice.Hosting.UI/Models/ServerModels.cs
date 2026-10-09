namespace Indice.Hosting.UI.Models;

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

/// <summary>A worker, as seen from the scheduled tasks it has run.</summary>
public class WorkerServerInfo
{
    /// <summary>The worker id (machine name).</summary>
    public string WorkerId { get; set; } = null!;
    /// <summary>The latest execution of its tasks.</summary>
    public DateTimeOffset? LastSeen { get; set; }
    /// <summary>The tasks whose last run was on this worker.</summary>
    public int Tasks { get; set; }
    /// <summary>The tasks currently running on this worker.</summary>
    public int RunningTasks { get; set; }
}
