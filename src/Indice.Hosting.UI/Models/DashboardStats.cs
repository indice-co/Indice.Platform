namespace Indice.Hosting.UI.Models;

/// <summary>Overall counters of the dashboard.</summary>
public class DashboardStats
{
    /// <summary>The server time when the counters were taken (UTC).</summary>
    public DateTime ServerTimeUtc { get; set; }
    /// <summary>Number of logical queues.</summary>
    public int Queues { get; set; }
    /// <summary>Messages waiting for their first attempt.</summary>
    public int Enqueued { get; set; }
    /// <summary>Delayed messages.</summary>
    public int Scheduled { get; set; }
    /// <summary>Messages waiting for another attempt.</summary>
    public int Retrying { get; set; }
    /// <summary>Poison messages.</summary>
    public int Poison { get; set; }
    /// <summary>Dequeued messages that are not yet cleaned up (obsolete EF store only).</summary>
    public int Dequeued { get; set; }
    /// <summary>Number of scheduled tasks.</summary>
    public int ScheduledTasks { get; set; }
    /// <summary>Scheduled tasks that are currently running.</summary>
    public int RunningTasks { get; set; }
    /// <summary>Scheduled tasks whose last run failed.</summary>
    public int FailingTasks { get; set; }
    /// <summary>Scheduled tasks that are disabled.</summary>
    public int DisabledTasks { get; set; }
    /// <summary>Locks that have not expired.</summary>
    public int ActiveLocks { get; set; }
    /// <summary>Number of workers that have run a scheduled task.</summary>
    public int Servers { get; set; }
}
