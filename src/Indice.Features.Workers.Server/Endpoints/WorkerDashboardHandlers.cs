using System.Security.Claims;
using Indice.Hosting;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Indice.Features.Workers.Server.Models;
using Indice.Features.Workers.Server.Services;

namespace Indice.Features.Workers.Server.Endpoints;

internal static class WorkerDashboardHandlers
{
    private const int MaxQueueNameLength = 400;
    private static readonly SemaphoreSlim StatsCacheLock = new(1, 1);

    public static async Task<Ok<WorkerDashboardPermissions>> GetPermissions(ClaimsPrincipal user, IAuthorizationService authorizationService) =>
        TypedResults.Ok(new WorkerDashboardPermissions {
            CanManage = (await authorizationService.AuthorizeAsync(user, WorkerPolicies.BeWorkersAdmin)).Succeeded
        });

    public static async Task<Ok<DashboardStats>> GetStats(IWorkerDashboardService service, IMemoryCache cache, IOptions<WorkerEndpointsOptions> options, CancellationToken cancellationToken) {
        var duration = options.Value.StatsCacheDuration;
        if (duration <= TimeSpan.Zero) {
            return TypedResults.Ok(await service.GetStats(cancellationToken));
        }
        // Every open dashboard polls the counters, so they are computed once per period for all of them.
        var key = GetStatsCacheKey(options.Value);
        if (cache.TryGetValue(key, out DashboardStats? cached) && cached is not null) {
            return TypedResults.Ok(cached);
        }
        await StatsCacheLock.WaitAsync(cancellationToken);
        try {
            if (cache.TryGetValue(key, out cached) && cached is not null) {
                return TypedResults.Ok(cached);
            }
            var stats = await service.GetStats(cancellationToken);
            cache.Set(key, stats, duration);
            return TypedResults.Ok(stats);
        } finally {
            StatsCacheLock.Release();
        }
    }

    public static async Task<Ok<List<QueueInfo>>> GetQueues(IWorkerDashboardService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetQueues(cancellationToken));

    public static async Task<Ok<QueueInfo>> GetQueue(string queueName, IWorkerDashboardService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetQueue(queueName, cancellationToken));

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
        IMemoryCache cache,
        IOptions<WorkerEndpointsOptions> options,
        CancellationToken cancellationToken
    ) {
        var affected = await service.RequeueMessages(request.Ids, cancellationToken);
        InvalidateStats(cache, options.Value);
        return TypedResults.Ok(new BulkActionResult { Affected = affected });
    }

    public static async Task<Ok<BulkActionResult>> DeleteMessages(
        MessageIdsRequest request,
        IWorkerDashboardService service,
        IMemoryCache cache,
        IOptions<WorkerEndpointsOptions> options,
        CancellationToken cancellationToken
    ) {
        var affected = await service.DeleteMessages(request.Ids, cancellationToken);
        InvalidateStats(cache, options.Value);
        return TypedResults.Ok(new BulkActionResult { Affected = affected });
    }

    public static async Task<Results<CreatedAtRoute<QueueMessageDetails>, ValidationProblem>> EnqueueMessage(
        string queueName,
        EnqueueMessageRequest request,
        IWorkerDashboardService service,
        IMemoryCache cache,
        IOptions<WorkerEndpointsOptions> options,
        CancellationToken cancellationToken
    ) {
        var error = ValidateQueueName(queueName);
        if (error is not null) {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { [nameof(queueName)] = [error] });
        }
        var message = await service.EnqueueMessage(queueName, request.Payload, request.VisibleAt, cancellationToken);
        InvalidateStats(cache, options.Value);
        return TypedResults.CreatedAtRoute(message, nameof(GetMessage), new { messageId = message.Id });
    }

    public static async Task<Results<Ok<QueueMessageDetails>, NotFound>> UpdateMessage(
        Guid messageId,
        UpdateMessageRequest request,
        IWorkerDashboardService service,
        IMemoryCache cache,
        IOptions<WorkerEndpointsOptions> options,
        CancellationToken cancellationToken
    ) {
        var message = await service.UpdateMessage(messageId, request.Payload, cancellationToken);
        if (message is null) {
            return TypedResults.NotFound();
        }
        InvalidateStats(cache, options.Value);
        return TypedResults.Ok(message);
    }

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
        IMemoryCache cache,
        IOptions<WorkerEndpointsOptions> options,
        CancellationToken cancellationToken
    ) {
        var updated = await service.SetScheduledTaskEnabled(taskId, request.Enabled, cancellationToken);
        if (!updated) {
            return TypedResults.NotFound();
        }
        InvalidateStats(cache, options.Value);
        return TypedResults.NoContent();
    }

    public static async Task<Results<Accepted<RunScheduledTaskResult>, NotFound>> RunScheduledTask(
        string taskId,
        ClaimsPrincipal user,
        IWorkerDashboardService service,
        CancellationToken cancellationToken
    ) {
        var requestedBy = user.FindFirst("name")?.Value ?? user.Identity?.Name ?? user.FindFirst("sub")?.Value;
        var result = await service.RunScheduledTask(taskId, requestedBy, cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Accepted((string?)null, result);
    }

    public static async Task<Ok<List<LockInfo>>> GetLocks(IWorkerDashboardService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetLocks(cancellationToken));

    public static async Task<Results<NoContent, NotFound>> ReleaseLock(
        Guid lockId,
        IWorkerDashboardService service,
        IMemoryCache cache,
        IOptions<WorkerEndpointsOptions> options,
        CancellationToken cancellationToken
    ) {
        if (!await service.ReleaseLock(lockId, cancellationToken)) {
            return TypedResults.NotFound();
        }
        InvalidateStats(cache, options.Value);
        return TypedResults.NoContent();
    }

    public static async Task<Ok<List<WorkerServerInfo>>> GetServers(IWorkerDashboardService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetServers(cancellationToken));

    private static string GetStatsCacheKey(WorkerEndpointsOptions options) => $"Indice.Workers.Dashboard.Stats:{options.PathPrefix}";

    private static void InvalidateStats(IMemoryCache cache, WorkerEndpointsOptions options) => cache.Remove(GetStatsCacheKey(options));

    private static string? ValidateQueueName(string queueName) {
        if (string.IsNullOrWhiteSpace(queueName)) {
            return "The queue name is required.";
        }
        if (queueName.Length > MaxQueueNameLength) {
            return $"The queue name cannot be longer than {MaxQueueNameLength} characters.";
        }
        if (queueName.StartsWith(HostingConventions.RunNowQueuePrefix, StringComparison.Ordinal)) {
            return "The queue is internal. Use the run endpoint of the scheduled task instead.";
        }
        if (queueName.EndsWith(HostingConventions.PoisonSuffix, StringComparison.Ordinal)) {
            return "Messages cannot be added to a poison queue.";
        }
        return null;
    }
}