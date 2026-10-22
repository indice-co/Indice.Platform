namespace Indice.Features.Workers.Server.Models;

/// <summary>Overall counters of the dashboard.</summary>
public class DashboardStats
{
    /// <summary>The server time when the counters were taken (UTC).</summary>
    public DateTime ServerTimeUtc { get; init; }
    
    /// <summary>Number of logical queues.</summary>
    public int Queues { get; init; }
    
    /// <summary>Messages waiting for their first attempt.</summary>
    public int Enqueued { get; init; }
    
    /// <summary>Delayed messages.</summary>
    public int Scheduled { get; init; }
    
    /// <summary>Messages waiting for another attempt.</summary>
    public int Retrying { get; init; }
    
    /// <summary>Poison messages.</summary>
    public int Poison { get; init; }
    
    /// <summary>Dequeued messages that are not yet cleaned up (obsolete EF store only).</summary>
    public int Dequeued { get; init; }
    
    /// <summary>Number of scheduled tasks.</summary>
    public int ScheduledTasks { get; init; }
    
    /// <summary>Scheduled tasks that are currently running.</summary>
    public int RunningTasks { get; init; }
    
    /// <summary>Scheduled tasks whose last run failed.</summary>
    public int FailingTasks { get; init; }
    
    /// <summary>Scheduled tasks that are disabled.</summary>
    public int DisabledTasks { get; init; }
    
    /// <summary>Locks that have not expired.</summary>
    public int ActiveLocks { get; init; }
    
    /// <summary>Number of workers that have run a scheduled task.</summary>
    public int Servers { get; init; }
}
