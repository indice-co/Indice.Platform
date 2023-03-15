using System.Net.Mime;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;

/// <summary>The sign in logs API.</summary>
public static class SignInLogApi
{
    /// <summary>Maps the sign in logs endpoints.</summary>
    /// <param name="builder">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static IEndpointRouteBuilder MapSignInLogs(this IEndpointRouteBuilder builder) {
        var group = builder.MapGroup("/api/")
                           .RequireAuthorization(BasicPolicyNames.BeAdmin)
                           .WithTags("SignInLogs");
        group.AddOpenApiSecurityRequirement("oauth2", "identity");
        // GET: /api/sign-in-logs
        group.MapGet("/sign-in-logs", async (
            [FromServices] ISignInLogService signInLogService,
            [AsParameters] ListOptions options
        ) => {
            var signInLogs = await signInLogService.ListAsync(options);
            return TypedResults.Ok(signInLogs);
        })
        .Produces<ResultSet<SignInLogEntry>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
        .ProducesProblem(StatusCodes.Status401Unauthorized, "application/problem+json")
        .ProducesProblem(StatusCodes.Status403Forbidden, "application/problem+json")
        .WithDescription("Gets the list of sign in logs produced by the Identity system.")
        .WithDisplayName("Get Sign in Logs")
        .WithName("GetSignInLogs")
        .WithSummary("Gets the list of sign in logs produced by the Identity system.");
        // PATCH: /api/sign-in-logs/{rowId}
        group.MapPatch("/sign-in-logs/{rowId}", async (
            [FromServices] ISignInLogService signInLogService,
            [FromRoute] Guid rowId,
            [FromBody] SignInLogEntryRequest model
        ) => {
            var rowsAffected = await signInLogService.UpdateAsync(rowId, model);
            return rowsAffected == 0 ? Results.NotFound() : Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound, "application/problem+json")
        .ProducesProblem(StatusCodes.Status401Unauthorized, "application/problem+json")
        .ProducesProblem(StatusCodes.Status403Forbidden, "application/problem+json")
        .WithDescription("Patches the specified log entry by updating the properties given in the request.")
        .WithDisplayName("Patches Log Entry.")
        .WithName("PatchSignInLog")
        .WithSummary("Patches the specified log entry by updating the properties given in the request.");
        return group;
    }
}
