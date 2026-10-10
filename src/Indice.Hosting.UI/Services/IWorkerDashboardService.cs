using Indice.Hosting.UI.Models;
using Indice.Types;

namespace Indice.Hosting.UI.Services;

/// <summary>Reads and changes the worker tables for the dashboard.</summary>
public interface IWorkerDashboardService
{
    /// <summary>Gets the overall counters.</summary>
    Task<DashboardStats> GetStats(CancellationToken cancellationToken = default);
    
    /// <summary>Gets the logical queues with their counters, ordered by name.</summary>
    Task<List<QueueInfo>> GetQueues(CancellationToken cancellationToken = default);
    
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
    
    /// <summary>Gets the scheduled tasks that have run at least once, ordered by group and name.</summary>
    Task<List<ScheduledTaskInfo>> GetScheduledTasks(CancellationToken cancellationToken = default);
    
    /// <summary>Gets a scheduled task with its errors and state, or <see langword="null"/> when it does not exist.</summary>
    Task<ScheduledTaskDetails?> GetScheduledTask(string taskId, CancellationToken cancellationToken = default);
    
    /// <summary>Enables or disables a scheduled task.</summary>
    /// <returns><see langword="false"/> when the task has no row (it has never run).</returns>
    Task<bool> SetScheduledTaskEnabled(string taskId, bool enabled, CancellationToken cancellationToken = default);
    
    /// <summary>Gets the locks, ordered by name.</summary>
    Task<List<LockInfo>> GetLocks(CancellationToken cancellationToken = default);
    
    /// <summary>Gets the workers that have run scheduled tasks, most recently seen first.</summary>
    Task<List<WorkerServerInfo>> GetServers(CancellationToken cancellationToken = default);
}
