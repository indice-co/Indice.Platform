using Indice.Hosting.UI.Models;
using Indice.Types;

namespace Indice.Hosting.UI.Services;

/// <summary>Reads and changes the worker tables (queue messages, scheduled tasks, locks) for the dashboard.</summary>
public interface IWorkerDashboardService
{
    /// <summary>Gets the overall counters.</summary>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    Task<DashboardStats> GetStatsAsync(CancellationToken cancellationToken = default);
    /// <summary>Gets the logical queues with their counters, ordered by name.</summary>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    Task<List<QueueInfo>> GetQueuesAsync(CancellationToken cancellationToken = default);
    /// <summary>Gets a page of the messages of a logical queue that have the given status.</summary>
    /// <param name="queueName">The logical queue name.</param>
    /// <param name="options">Paging, search and status filter.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    Task<ResultSet<QueueMessageListItem>> GetQueueMessagesAsync(string queueName, QueueMessageListOptions options, CancellationToken cancellationToken = default);
    /// <summary>Gets a message, or <see langword="null"/> when it is gone (processed messages are deleted).</summary>
    /// <param name="messageId">The message id.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    Task<QueueMessageDetails?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
    /// <summary>Makes the messages visible now, resets their attempts and moves poison messages back to their queue.</summary>
    /// <param name="ids">The message ids.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>The number of messages affected.</returns>
    Task<int> RequeueMessagesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    /// <summary>Deletes the messages.</summary>
    /// <param name="ids">The message ids.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>The number of messages deleted.</returns>
    Task<int> DeleteMessagesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    /// <summary>Gets the scheduled tasks that have run at least once, ordered by group and name.</summary>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    Task<List<ScheduledTaskInfo>> GetScheduledTasksAsync(CancellationToken cancellationToken = default);
    /// <summary>Gets a scheduled task with its errors and state, or <see langword="null"/> when it does not exist.</summary>
    /// <param name="taskId">The task id, <c>{group}.{name}</c>.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    Task<ScheduledTaskDetails?> GetScheduledTaskAsync(string taskId, CancellationToken cancellationToken = default);
    /// <summary>Enables or disables a scheduled task.</summary>
    /// <param name="taskId">The task id, <c>{group}.{name}</c>.</param>
    /// <param name="enabled">Whether the task runs on its schedule.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns><see langword="false"/> when the task has no row (it has never run).</returns>
    Task<bool> SetScheduledTaskEnabledAsync(string taskId, bool enabled, CancellationToken cancellationToken = default);
    /// <summary>Gets the locks, ordered by name.</summary>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    Task<List<LockInfo>> GetLocksAsync(CancellationToken cancellationToken = default);
    /// <summary>Gets the workers that have run scheduled tasks, most recently seen first.</summary>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    Task<List<WorkerServerInfo>> GetServersAsync(CancellationToken cancellationToken = default);
}
