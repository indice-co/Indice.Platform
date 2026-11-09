using System.Security.Claims;
using Indice.Features.ActivityLogs;
using Indice.Features.ActivityLogs.Models;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.Server.ActivityLog;

/// <summary>The activity logs API.</summary>
public static class ActivityLogsApi
{
    /// <summary>Maps the activity logs endpoints.</summary>
    /// <param name="builder">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static IEndpointRouteBuilder MapActivityLogs(this IEndpointRouteBuilder builder) {
        var isFeatureRegistered = builder.ServiceProvider.GetService<ActivityLogEntryQueue>() is not null;
        var options = builder.GetEndpointOptions<ActivityLogOptions>();
        if (!isFeatureRegistered || !options.Enable) {
            return builder;
        }
        var allowedScopes = new[] {
            IdentityEndpoints.SubScopes.Logs
        }
        .Where(x => x is not null)
        .Cast<string>()
        .ToArray();
        var group = builder
            .MapGroup($"{options.ApiPrefix}/")
            .WithGroupName("identity")
            .WithTags("ActivityLogs")
            .RequireAuthorization(policy => policy
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
            )
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        group.WithOpenApiSecurityRequirement("oauth2", allowedScopes);

        //Activity Logs
        // GET: /api/activity-logs
        group.MapGet("activity-logs", async (
            IActivityLogStore activityLogStore,
            [AsParameters] ListOptions options,
            [AsParameters] ActivityLogEntryFilter filter
        ) => {
            var activityLogs = await activityLogStore.ListAsync(options, filter);
            return TypedResults.Ok(activityLogs);
        })
        .Produces<ResultSet<ActivityLogEntry>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithName("GetActivityLogs")
        .WithSummary("Gets the list of activity logs produced by the Identity system.")
        .RequireAuthorization(IdentityEndpoints.Policies.BeLogsReader);

        // GET: /api/my/activity-logs
        group.MapGet("my/activity-logs", async (
            ClaimsPrincipal currentUser,
            IActivityLogStore activityLogStore,
            [AsParameters] ListOptions options,
            [AsParameters] ActivityLogEntryFilterBase filter
        ) => {
            if (options.Size > 100) {
                return TypedResults.ValidationProblem(ValidationErrors.AddError("size", "Max allowed value for page size is 100."));
            }
            var activityLogs = await activityLogStore.ListAsync(options, new ActivityLogEntryFilter {
                From = filter.From,
                To = filter.To,
                ApplicationId = filter.ApplicationId,
                Subject = currentUser.FindSubjectId()
            });
            return Results.Ok(activityLogs);
        })
        .Produces<ResultSet<ActivityLogEntry>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithName("GetMyActivityLogs")
        .WithSummary("Gets the list of activity logs for the current user.");

        // PATCH: /api/activity-logs/{rowId}
        group.MapPatch("activity-logs/{rowId}", async (
            IActivityLogStore activityLogStore,
            Guid rowId,
            ActivityLogEntryRequest model
        ) => {
            var rowsAffected = await activityLogStore.UpdateAsync(rowId, model);
            return rowsAffected == 0 ? Results.NotFound() : Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithName("PatchActivityLog")
        .WithSummary("Patches the specified log entry by updating the properties given in the request.")
        .RequireAuthorization(IdentityEndpoints.Policies.BeLogsWriter);

        return group;
    }
}
