using Indice.Hosting.UI.Models;
using Indice.Hosting.UI.Services;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Hosting.UI.Endpoints;

/// <summary>Logic-free handlers (command and query invokers).</summary>
/// <remarks>Their shape will enforce open api documentation without the need to add explicit annotations and metadata.
/// So Union types are used to represent multiple possible outcomes and <see cref="TypedResults"/> are always prefered over raw results.</remarks>
internal static class WorkerDashboardHandlers
{
    /// <summary>GET /api/workers/stats — overall counters.</summary>
    public static async Task<Ok<DashboardStats>> GetStats(IWorkerDashboardService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetStatsAsync(cancellationToken));

    /// <summary>GET /api/workers/queues — logical queues with their counters.</summary>
    public static async Task<Ok<List<QueueInfo>>> GetQueues(IWorkerDashboardService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetQueuesAsync(cancellationToken));

    /// <summary>GET /api/workers/queues/{queueName}/messages — a page of the messages of a queue with the given status.</summary>
    public static async Task<Ok<ResultSet<QueueMessageListItem>>> GetQueueMessages(string queueName, [AsParameters] QueueMessageListOptions options,
        IWorkerDashboardService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetQueueMessagesAsync(queueName, options, cancellationToken));

    /// <summary>GET /api/workers/messages/{messageId} — a message with its full payload.</summary>
    public static async Task<Results<Ok<QueueMessageDetails>, NotFound>> GetMessage(Guid messageId, IWorkerDashboardService service, CancellationToken cancellationToken) {
        var message = await service.GetMessageAsync(messageId, cancellationToken);
        return message is null ? TypedResults.NotFound() : TypedResults.Ok(message);
    }

    /// <summary>POST /api/workers/messages/requeue — makes messages visible now and moves poison messages back to their queue.</summary>
    public static async Task<Ok<BulkActionResult>> RequeueMessages(MessageIdsRequest request, IWorkerDashboardService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(new BulkActionResult {
            Affected = await service.RequeueMessagesAsync(request.Ids, cancellationToken)
        });

    /// <summary>POST /api/workers/messages/delete — deletes messages.</summary>
    public static async Task<Ok<BulkActionResult>> DeleteMessages(MessageIdsRequest request, IWorkerDashboardService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(new BulkActionResult { Affected = await service.DeleteMessagesAsync(request.Ids, cancellationToken) });

    /// <summary>GET /api/workers/scheduled-tasks — the scheduled tasks.</summary>
    public static async Task<Ok<List<ScheduledTaskInfo>>> GetScheduledTasks(IWorkerDashboardService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetScheduledTasksAsync(cancellationToken));

    /// <summary>GET /api/workers/scheduled-tasks/{taskId} — a scheduled task with its errors and state.</summary>
    public static async Task<Results<Ok<ScheduledTaskDetails>, NotFound>> GetScheduledTask(string taskId, IWorkerDashboardService service, CancellationToken cancellationToken) {
        var task = await service.GetScheduledTaskAsync(taskId, cancellationToken);
        return task is null ? TypedResults.NotFound() : TypedResults.Ok(task);
    }

    /// <summary>PUT /api/workers/scheduled-tasks/{taskId}/enabled — enables or disables a scheduled task.</summary>
    public static async Task<Results<NoContent, NotFound>> SetScheduledTaskEnabled(string taskId, SetScheduledTaskEnabledRequest request,
        IWorkerDashboardService service, CancellationToken cancellationToken) {
        var updated = await service.SetScheduledTaskEnabledAsync(taskId, request.Enabled, cancellationToken);
        return updated ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    /// <summary>GET /api/workers/locks — the distributed locks.</summary>
    public static async Task<Ok<List<LockInfo>>> GetLocks(IWorkerDashboardService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetLocksAsync(cancellationToken));

    /// <summary>GET /api/workers/servers — the workers that have run scheduled tasks.</summary>
    public static async Task<Ok<List<WorkerServerInfo>>> GetServers(IWorkerDashboardService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetServersAsync(cancellationToken));
}
