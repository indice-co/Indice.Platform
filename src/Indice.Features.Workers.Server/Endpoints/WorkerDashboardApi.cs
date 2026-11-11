using Indice.Features.Workers.Server;
using Indice.Features.Workers.Server.Endpoints;
using Indice.Features.Workers.Server.Models;
using Indice.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>HTTP surface of the workers dashboard: queues and messages, scheduled tasks, locks and servers.</summary>
internal static class WorkerDashboardApi
{
    /// <summary>Maps the <c>/api/workers</c> endpoint group.</summary>
    public static RouteGroupBuilder MapWorkerDashboardApi(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<WorkerEndpointsOptions>>().Value;
        var allowedScopes = new[] { options.RequiredScope }.FilterOutNulls().ToArray();

        var group = routes.MapGroup(options.PathPrefix.Value?.TrimEnd('/') ?? string.Empty)
                          .WithGroupName(options.GroupName)
                          .WithTags("Workers");

        // Reading needs the reader policy. Every endpoint that changes something also needs the admin policy.
        group.RequireAuthorization(WorkerPolicies.BeWorkersReader);

        group.WithOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("me", WorkerDashboardHandlers.GetPermissions)
             .WithName(nameof(WorkerDashboardHandlers.GetPermissions))
             .WithSummary("Get the permissions of the current user.")
             .WithDescription("Returns whether the current user can change queues, scheduled tasks and locks, so that the UI can hide the actions it cannot use.");

        group.MapGet("stats", WorkerDashboardHandlers.GetStats)
             .WithName(nameof(WorkerDashboardHandlers.GetStats))
             .WithSummary("Get the overall counters.")
             .WithDescription("Returns the message counters of all the queues, the scheduled task counters, the active locks and the number of workers.");

        group.MapGet("queues", WorkerDashboardHandlers.GetQueues)
             .WithName(nameof(WorkerDashboardHandlers.GetQueues))
             .WithSummary("List the queues.")
             .WithDescription("Returns the logical queues ordered by name, with a counter per message status. Poison queues are merged into normal.");

        group.MapGet("queues/{queueName}", WorkerDashboardHandlers.GetQueue)
             .WithName(nameof(WorkerDashboardHandlers.GetQueue))
             .WithSummary("Get a queue.")
             .WithDescription("Returns the counters of one logical queue, its poison queue included. A queue without messages has zero counters.");

        group.MapGet("queues/{queueName}/messages", WorkerDashboardHandlers.GetQueueMessages)
             .WithName(nameof(WorkerDashboardHandlers.GetQueueMessages))
             .WithSummary("List the messages of a queue.")
             .WithDescription("Returns a page of the messages of a logical queue that have the given status, in the order they become visible. `search` matches a message id or a part of the payload. `sort` is ignored.");

        group.MapPost("queues/{queueName}/messages", WorkerDashboardHandlers.EnqueueMessage)
             .RequireAuthorization(WorkerPolicies.BeWorkersAdmin)
             .WithParameterValidation<EnqueueMessageRequest>()
             .WithName(nameof(WorkerDashboardHandlers.EnqueueMessage))
             .WithSummary("Add a message to a queue.")
             .WithDescription("Adds a message with the given JSON payload. The payload must match the work item type of the queue, otherwise the worker fails to process it and the message ends up as poison.");

        group.MapGet("messages/{messageId:guid}", WorkerDashboardHandlers.GetMessage)
             .WithName(nameof(WorkerDashboardHandlers.GetMessage))
             .WithSummary("Get a message.")
             .WithDescription("Returns a message with its full payload. Returns 404 when the message is gone: processed messages are deleted from the queue.");

        group.MapPut("messages/{messageId:guid}", WorkerDashboardHandlers.UpdateMessage)
             .RequireAuthorization(WorkerPolicies.BeWorkersAdmin)
             .WithParameterValidation<UpdateMessageRequest>()
             .WithName(nameof(WorkerDashboardHandlers.UpdateMessage))
             .WithSummary("Edit the payload of a message and requeue it.")
             .WithDescription("Replaces the payload, makes the message visible now and resets its attempts. A poison message moves back to its queue. Returns 404 when the message is gone.");

        group.MapPost("messages/requeue", WorkerDashboardHandlers.RequeueMessages)
             .RequireAuthorization(WorkerPolicies.BeWorkersAdmin)
             .WithParameterValidation<MessageIdsRequest>()
             .WithName(nameof(WorkerDashboardHandlers.RequeueMessages))
             .WithSummary("Requeue messages.")
             .WithDescription("Makes the messages visible now and resets their attempts. Poison messages move back to their queue. Messages already processed by a worker are not affected.");

        group.MapPost("messages/delete", WorkerDashboardHandlers.DeleteMessages)
             .RequireAuthorization(WorkerPolicies.BeWorkersAdmin)
             .WithParameterValidation<MessageIdsRequest>()
             .WithName(nameof(WorkerDashboardHandlers.DeleteMessages))
             .WithSummary("Delete messages.")
             .WithDescription("Permanently deletes the messages. Returns the number of messages deleted.");

        group.MapGet("scheduled-tasks", WorkerDashboardHandlers.GetScheduledTasks)
             .WithName(nameof(WorkerDashboardHandlers.GetScheduledTasks))
             .WithSummary("List the scheduled tasks.")
             .WithDescription("Returns the scheduled tasks ordered by group and name. A task appears after its first execution.");

        group.MapGet("scheduled-tasks/{taskId}", WorkerDashboardHandlers.GetScheduledTask)
             .WithName(nameof(WorkerDashboardHandlers.GetScheduledTask))
             .WithSummary("Get a scheduled task.")
             .WithDescription("Returns a scheduled task with the text of its last error and its state.");

        group.MapPut("scheduled-tasks/{taskId}/enabled", WorkerDashboardHandlers.SetScheduledTaskEnabled)
             .RequireAuthorization(WorkerPolicies.BeWorkersAdmin)
             .WithName(nameof(WorkerDashboardHandlers.SetScheduledTaskEnabled))
             .WithSummary("Enable or disable a scheduled task.")
             .WithDescription("A disabled task skips its scheduled executions. Returns 404 when the task has never run.");

        group.MapPost("scheduled-tasks/{taskId}/run", WorkerDashboardHandlers.RunScheduledTask)
             .RequireAuthorization(WorkerPolicies.BeWorkersAdmin)
             .WithName(nameof(WorkerDashboardHandlers.RunScheduledTask))
             .WithSummary("Run a scheduled task now.")
             .WithDescription("Requests a run outside the schedule; it also runs a disabled task. A worker that hosts the task picks the request up within its polling interval (15 seconds by default) and requests older than 10 minutes are discarded. When a request is already pending, it is returned. Returns 404 when the task has never run.");

        group.MapGet("locks", WorkerDashboardHandlers.GetLocks)
             .WithName(nameof(WorkerDashboardHandlers.GetLocks))
             .WithSummary("List the locks.")
             .WithDescription("Returns the distributed locks ordered by name. Expired locks are cleaned up lazily, so they may still be listed.");

        group.MapDelete("locks/{lockId:guid}", WorkerDashboardHandlers.ReleaseLock)
             .RequireAuthorization(WorkerPolicies.BeWorkersAdmin)
             .WithName(nameof(WorkerDashboardHandlers.ReleaseLock))
             .WithSummary("Force release a lock.")
             .WithDescription("Deletes the lock. The holder can no longer renew it, so a singleton task that holds it is cancelled at its next renewal, and another worker can acquire it. Returns 404 when the lock is gone.");

        group.MapGet("servers", WorkerDashboardHandlers.GetServers)
             .WithName(nameof(WorkerDashboardHandlers.GetServers))
             .WithSummary("List the workers.")
             .WithDescription("Returns the workers that ran scheduled tasks, most recently seen first.");

        return group;
    }
}
