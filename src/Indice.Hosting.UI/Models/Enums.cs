using System.Text.Json.Serialization;

namespace Indice.Hosting.UI.Models;

/// <summary>The status of a queue message, as shown by the dashboard. Statuses are mutually exclusive.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<QueueMessageStatus>))]
public enum QueueMessageStatus
{
    /// <summary>Visible and waiting for its first attempt.</summary>
    Enqueued,
    /// <summary>Delayed: not visible before its date.</summary>
    Scheduled,
    /// <summary>Visible and waiting for another attempt after a failure.</summary>
    Retrying,
    /// <summary>Failed more times than allowed and moved aside.</summary>
    Poison,
    /// <summary>Processed but not yet cleaned up. Only produced by the obsolete EF queue store.</summary>
    Dequeued
}

/// <summary>The state of a scheduled task.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ScheduledTaskState>))]
public enum ScheduledTaskState
{
    /// <summary>Waiting for its next execution.</summary>
    Idle,
    /// <summary>Currently running.</summary>
    Running
}
