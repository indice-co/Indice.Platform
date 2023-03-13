namespace Indice.Hosting.Models;

/// <summary>The default implementation for the <see cref="ScheduledTask{TState}"/> with state as <see cref="Dictionary{String, Object}"/></summary>
public class ScheduledTask : ScheduledTask<Dictionary<string, object>> { }

/// <summary>A DTO representing a worker task.</summary>
/// <typeparam name="TState"></typeparam>
public class ScheduledTask<TState> where TState : class
{
    /// <summary>The id.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>The worker id.</summary>
    public string WorkerId { get; set; }
    /// <summary>Task group.</summary>
    public string Group { get; set; }
    /// <summary>Task name.</summary>
    public string Description { get; set; }
    /// <summary>The type name.</summary>
    public string Type { get; set; }
    /// <summary>The date.</summary>
    public DateTimeOffset LastExecution { get; set; }
    /// <summary>The date.</summary>
    public DateTimeOffset? NextExecution { get; set; }
    /// <summary>The date.</summary>
    public int ExecutionCount { get; set; }
    /// <summary>The status.</summary>
    public ScheduledTaskStatus Status { get; set; }
    /// <summary>The errors.</summary>
    public string Errors { get; set; }
    /// <summary>The last time an error occurred.</summary>
    public DateTimeOffset? LastErrorDate { get; set; }
    /// <summary>The payload.</summary>
    public TState State { get; set; }
    /// <summary>The status.</summary>
    public double Progress { get; set; }
    /// <summary>If this is set to false the schedule will be disabled</summary>
    public bool Enabled { get; set; }
}
