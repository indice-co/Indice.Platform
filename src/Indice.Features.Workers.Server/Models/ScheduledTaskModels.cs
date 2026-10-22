namespace Indice.Features.Workers.Server.Models;

/// <summary>A scheduled (cron) task.</summary>
public class ScheduledTaskInfo
{
    /// <summary>The task id, <c>{group}.{name}</c>.</summary>
    public string Id { get; set; } = null!;
    /// <summary>The task name.</summary>
    public string Name { get; set; } = null!;
    /// <summary>The task group.</summary>
    public string? Group { get; set; }
    /// <summary>The task description.</summary>
    public string? Description { get; set; }
    /// <summary>The handler type name, when it can be read from the stored job type; otherwise the stored job type.</summary>
    public string? JobType { get; set; }
    /// <summary>The cron expression. Only known in the worker process, so it is <see langword="null"/> in the dashboard host.</summary>
    public string? CronExpression { get; set; }
    /// <summary>The task state.</summary>
    public ScheduledTaskState Status { get; set; }
    /// <summary>Whether the task runs on its schedule.</summary>
    public bool Enabled { get; set; }
    /// <summary>Whether the task has run at least once (a database row exists).</summary>
    public bool HasRun { get; set; }
    /// <summary>The fire time of the last execution.</summary>
    public DateTimeOffset? LastExecution { get; set; }
    /// <summary>The next fire time.</summary>
    public DateTimeOffset? NextExecution { get; set; }
    /// <summary>The number of executions.</summary>
    public int ExecutionCount { get; set; }
    /// <summary>The last time the task failed.</summary>
    public DateTimeOffset? LastErrorDate { get; set; }
    /// <summary>Whether the last execution failed.</summary>
    public bool LastRunFailed { get; set; }
    /// <summary>The progress reported by the task.</summary>
    public double Progress { get; set; }
    /// <summary>The worker that ran the task last.</summary>
    public string? WorkerId { get; set; }
}

/// <summary>A scheduled task with its errors and state.</summary>
public class ScheduledTaskDetails : ScheduledTaskInfo
{
    /// <summary>Whether only one worker may run the task at a time. Only known in the worker process.</summary>
    public bool? Singleton { get; set; }
    /// <summary>The full text of the last error.</summary>
    public string? Errors { get; set; }
    /// <summary>The task state, as a JSON string.</summary>
    public string? State { get; set; }
}

/// <summary>Enables or disables a scheduled task.</summary>
public class SetScheduledTaskEnabledRequest
{
    /// <summary>Whether the task runs on its schedule.</summary>
    public bool Enabled { get; set; }
}
