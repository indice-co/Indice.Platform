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
    /// <summary>The handler type. Only known in the worker process, so it is <see langword="null"/> in the dashboard host.</summary>
    public string? HandlerType { get; set; }
    /// <summary>The polling interval in milliseconds. Only known in the worker process.</summary>
    public double? PollingInterval { get; set; }
    /// <summary>The number of dequeue instances. Only known in the worker process.</summary>
    public int? InstanceCount { get; set; }
    /// <summary>The maximum number of attempts before a message becomes poison. Only known in the worker process.</summary>
    public int? MaxRetryCount { get; set; }
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
    /// <summary>The maximum number of attempts before a message becomes poison. Only known in the worker process.</summary>
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
