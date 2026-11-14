using Indice.Types;

namespace Indice.Features.Workers.Server.Models;

/// <summary>A logical queue. The <c>{name}-poison</c> queue is folded into its <c>{name}</c> queue.</summary>
public class QueueInfo
{
    /// <summary>The logical queue name (without the <c>-poison</c> suffix).</summary>
    public string Name { get; set; } = null!;
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
    /// <summary>All the messages of the queue.</summary>
    public int Total { get; set; }
    /// <summary>The oldest date of the visible messages (enqueued or retrying), in UTC. Indicates the lag of the queue.</summary>
    public DateTime? OldestEnqueuedAt { get; set; }
    /// <summary>Whether a live worker consumes the queue.</summary>
    /// <remarks>The settings below come from the registered workers that consume the queue, preferably the live ones. They are <see langword="null"/> when no registered worker declares the queue.</remarks>
    public bool IsRegistered { get; set; }
    /// <summary>The number of live workers that consume the queue. <see langword="null"/> when the server table is not available.</summary>
    public int? Consumers { get; set; }
    /// <summary>Whether the live workers that consume the queue have different settings, e.g. during a rolling deployment.</summary>
    public bool HasConfigurationMismatch { get; set; }
    /// <summary>The work item type.</summary>
    public string? WorkItemType { get; set; }
    /// <summary>The handler type.</summary>
    public string? HandlerType { get; set; }
    /// <summary>The polling interval in milliseconds.</summary>
    public double? PollingInterval { get; set; }
    /// <summary>The number of dequeue instances across the live workers.</summary>
    public int? InstanceCount { get; set; }
    /// <summary>The maximum number of attempts before a message becomes poison.</summary>
    public int? MaxRetryCount { get; set; }
    /// <summary>The messages the live workers are processing now.</summary>
    /// <remarks>
    /// The activity below is measured by the live workers and stored with their heartbeat, so it is up to a heartbeat old and covers about the last minute.
    /// It is <see langword="null"/> when no live worker reports metrics for the queue.
    /// </remarks>
    public long? Processing { get; set; }
    /// <summary>The messages processed per minute by the live workers, successfully or not.</summary>
    public double? ProcessedPerMinute { get; set; }
    /// <summary>The messages that failed per minute (retried or poisoned).</summary>
    public double? FailedPerMinute { get; set; }
    /// <summary>The average handler time, in milliseconds. <see langword="null"/> when no message was processed recently.</summary>
    public double? AverageDuration { get; set; }
    /// <summary>The average time the messages waited in the queue before a worker dequeued them, in milliseconds. <see langword="null"/> when no message was dequeued recently.</summary>
    public double? AverageWait { get; set; }
}

/// <summary>A queue message, as shown in a list.</summary>
public class QueueMessageListItem
{
    /// <summary>The message id.</summary>
    public Guid Id { get; set; }
    /// <summary>The physical queue name (may end with <c>-poison</c>).</summary>
    public string QueueName { get; set; } = null!;
    /// <summary>The message status.</summary>
    public QueueMessageStatus Status { get; set; }
    /// <summary>The date the message becomes visible, in UTC.</summary>
    public DateTime Date { get; set; }
    /// <summary>The number of failed attempts.</summary>
    public int DequeueCount { get; set; }
    /// <summary>The first 200 characters of the payload.</summary>
    public string? PayloadPreview { get; set; }
}

/// <summary>A queue message with its full payload.</summary>
public class QueueMessageDetails : QueueMessageListItem
{
    /// <summary>The logical queue name (without the <c>-poison</c> suffix).</summary>
    public string LogicalQueueName { get; set; } = null!;
    /// <summary>The full JSON payload.</summary>
    public string? Payload { get; set; }
    /// <summary>The maximum number of attempts before a message becomes poison. Only known when a registered worker consumes the queue.</summary>
    public int? MaxRetryCount { get; set; }
}

/// <summary>Paging, search and status filter for the messages of a queue.</summary>
public class QueueMessageListOptions : ListOptions
{
    /// <summary>The status to list. Defaults to <see cref="QueueMessageStatus.Enqueued"/>.</summary>
    public QueueMessageStatus Status { get; set; } = QueueMessageStatus.Enqueued;
}

/// <summary>A set of message ids for a bulk action.</summary>
public class MessageIdsRequest
{
    /// <summary>The message ids (1 to 1000).</summary>
    public Guid[] Ids { get; set; } = [];
}

/// <summary>The outcome of a bulk action.</summary>
public class BulkActionResult
{
    /// <summary>The number of messages affected. Messages already processed by a worker are not counted.</summary>
    public int Affected { get; set; }
}

/// <summary>A new message to put in a queue.</summary>
public class EnqueueMessageRequest
{
    /// <summary>The JSON payload. It must match the work item type of the queue, as serialized by the worker.</summary>
    public string Payload { get; set; } = null!;
    /// <summary>When the message becomes visible, in UTC. Leave empty to make it visible now.</summary>
    public DateTime? VisibleAt { get; set; }
}

/// <summary>A new payload for a message.</summary>
public class UpdateMessageRequest
{
    /// <summary>The JSON payload. It must match the work item type of the queue, as serialized by the worker.</summary>
    public string Payload { get; set; } = null!;
}
