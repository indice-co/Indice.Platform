using Indice.Hosting.UI.Models;
using Indice.Hosting.UI.Services;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Hosting.UI.Endpoints;

internal static class WorkerDashboardHandlers
{
    public static async Task<Ok<DashboardStats>> GetStats(IWorkerDashboardService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetStats(cancellationToken));

    public static async Task<Ok<List<QueueInfo>>> GetQueues(IWorkerDashboardService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetQueues(cancellationToken));

    public static async Task<Ok<ResultSet<QueueMessageListItem>>> GetQueueMessages(
        string queueName,
        [AsParameters] QueueMessageListOptions options,
        IWorkerDashboardService service,
        CancellationToken cancellationToken
    ) => TypedResults.Ok(await service.GetQueueMessages(queueName, options, cancellationToken));

    public static async Task<Results<Ok<QueueMessageDetails>, NotFound>> GetMessage(
        Guid messageId,
        IWorkerDashboardService service,
        CancellationToken cancellationToken
    ) {
        var message = await service.GetMessage(messageId, cancellationToken);
        return message is null ? TypedResults.NotFound() : TypedResults.Ok(message);
    }

    public static async Task<Ok<BulkActionResult>> RequeueMessages(
        MessageIdsRequest request,
        IWorkerDashboardService service,
        CancellationToken cancellationToken
    ) => TypedResults.Ok(new BulkActionResult {
        Affected = await service.RequeueMessages(request.Ids, cancellationToken)
    });

    public static async Task<Ok<BulkActionResult>> DeleteMessages(
        MessageIdsRequest request,
        IWorkerDashboardService service,
        CancellationToken cancellationToken
    ) => TypedResults.Ok(new BulkActionResult {
        Affected = await service.DeleteMessages(request.Ids, cancellationToken)
    });

    public static async Task<Ok<List<ScheduledTaskInfo>>> GetScheduledTasks(
        IWorkerDashboardService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetScheduledTasks(cancellationToken));

    public static async Task<Results<Ok<ScheduledTaskDetails>, NotFound>> GetScheduledTask(
        string taskId,
        IWorkerDashboardService service,
        CancellationToken cancellationToken
    ) {
        var task = await service.GetScheduledTask(taskId, cancellationToken);
        return task is null ? TypedResults.NotFound() : TypedResults.Ok(task);
    }

    public static async Task<Results<NoContent, NotFound>> SetScheduledTaskEnabled(
        string taskId,
        SetScheduledTaskEnabledRequest request,
        IWorkerDashboardService service,
        CancellationToken cancellationToken
    ) {
        var updated = await service.SetScheduledTaskEnabled(taskId, request.Enabled, cancellationToken);
        return updated ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    public static async Task<Ok<List<LockInfo>>> GetLocks(IWorkerDashboardService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetLocks(cancellationToken));

    public static async Task<Ok<List<WorkerServerInfo>>> GetServers(IWorkerDashboardService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetServers(cancellationToken));
}