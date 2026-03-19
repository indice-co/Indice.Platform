using System.Security.Claims;
using Indice.Features.ActivityLogs;
using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Models;
using Indice.Features.Messages.Core;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.AspNetCore.Endpoints;

/// <summary>
/// Api to interact with activity logs
/// </summary>
public static class ActivityLogsApi
{
    /// <summary>Maps the activity logs endpoints.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static IEndpointRouteBuilder MapActivityLogs(this IEndpointRouteBuilder routes) {

        // GET: /api/activity-logs
        routes.MapGet("activity-logs", async (
            ClaimsPrincipal currentUser,
            IActivityLogStore activityLogStore,
            [AsParameters] ListOptions options,
            [AsParameters] ActivityLogEntryFilter filter
        ) => {
            if (options.Size > 100) {
                return TypedResults.ValidationProblem(ValidationErrors.AddError("size", "Max allowed value for page size is 100."));
            }
            var signInLogs = await activityLogStore.ListAsync(options, filter);
            return Results.Ok(signInLogs);
        })
        .Produces<ResultSet<ActivityLogEntry>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithName("GetActivityLogs")
        .WithSummary("Gets the list of activity logs.");
        return routes;
    }
}
