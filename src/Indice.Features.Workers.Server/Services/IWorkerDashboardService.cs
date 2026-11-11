using Indice.Features.Workers.Server.Models;
using Indice.Types;

namespace Indice.Features.Workers.Server.Services;

/// <summary>Reads and changes the worker tables for the dashboard.</summary>
public interface IWorkerDashboardService
{
    /// <summary>Gets the overall counters.</summary>
    Task<DashboardStats> GetStats(CancellationToken cancellationToken = default);
    
    /// <summary>Gets the logical queues with their counters, ordered by name.</summary>
    Task<List<QueueInfo>> GetQueues(CancellationToken cancellationToken = default);

    /// <summary>Gets the counters of one logical queue. A queue without messages has zero counters.</summary>
    Task<QueueInfo> GetQueue(string queueName, CancellationToken cancellationToken = default);
    
    /// <summary>Gets a page of the messages of a logical queue that have the given status.</summary>
    Task<ResultSet<QueueMessageListItem>> GetQueueMessages(string queueName, QueueMessageListOptions options, CancellationToken cancellationToken = default);
    
    /// <summary>Gets a message.</summary>
    Task<QueueMessageDetails?> GetMessage(Guid messageId, CancellationToken cancellationToken = default);
    
    // todo: allow only poison to back transition OR leave as functionality and make explicit in UI.
    /// <summary>
    /// Makes a bunch of queue messages <b>immediately</b> visible.
    /// <remarks>
    /// This will:<br/>
    /// 1. Reset the attempts of a running queue message.<br/>
    /// 2. Move poison messages back to their queue.<br/>
    /// 3. Change the Date of scheduled messages to now.<br/>
    /// This runs the whole aggregation of actions, essentially updating the message to appear as a brand-new insertion.
    /// </remarks>
    /// </summary>
    Task<int> RequeueMessages(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    
    /// <summary>Deletes the messages.</summary>
    Task<int> DeleteMessages(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>Puts a new message in a queue.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="payload">The JSON payload.</param>
    /// <param name="visibleAt">When the message becomes visible (UTC). <see langword="null"/> for now.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<QueueMessageDetails> EnqueueMessage(string queueName, string payload, DateTime? visibleAt, CancellationToken cancellationToken = default);

    /// <summary>Replaces the payload of a message and requeues it, as <see cref="RequeueMessages"/> does.</summary>
    /// <returns>The updated message, or <see langword="null"/> when the message is gone.</returns>
    Task<QueueMessageDetails?> UpdateMessage(Guid messageId, string payload, CancellationToken cancellationToken = default);
    
    /// <summary>Gets the scheduled tasks that have run at least once, ordered by group and name.</summary>
    Task<List<ScheduledTaskInfo>> GetScheduledTasks(CancellationToken cancellationToken = default);
    
    /// <summary>Gets a scheduled task with its errors and state, or <see langword="null"/> when it does not exist.</summary>
    Task<ScheduledTaskDetails?> GetScheduledTask(string taskId, CancellationToken cancellationToken = default);
    
    /// <summary>Enables or disables a scheduled task.</summary>
    /// <returns><see langword="false"/> when the task has no row (it has never run).</returns>
    Task<bool> SetScheduledTaskEnabled(string taskId, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests a run of a scheduled task now. A worker that hosts the task picks the request up within its <c>RunNowPollingInterval</c>.
    /// When a request is already pending, it is returned instead of adding another one.
    /// </summary>
    /// <param name="taskId">The task id.</param>
    /// <param name="requestedBy">Who requested the run, kept for troubleshooting.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The pending request, or <see langword="null"/> when the task has no row (it has never run).</returns>
    Task<RunScheduledTaskResult?> RunScheduledTask(string taskId, string? requestedBy = null, CancellationToken cancellationToken = default);
    
    /// <summary>Gets the locks, ordered by name.</summary>
    Task<List<LockInfo>> GetLocks(CancellationToken cancellationToken = default);

    /// <summary>
    /// Force releases a lock. The holder cannot renew it anymore, so a singleton task is cancelled at its next renewal and another worker can take over.
    /// </summary>
    /// <returns><see langword="false"/> when the lock does not exist.</returns>
    Task<bool> ReleaseLock(Guid lockId, CancellationToken cancellationToken = default);
    
    /// <summary>Gets the workers that have run scheduled tasks, most recently seen first.</summary>
    Task<List<WorkerServerInfo>> GetServers(CancellationToken cancellationToken = default);
}
