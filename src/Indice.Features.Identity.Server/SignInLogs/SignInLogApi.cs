﻿using System.Net.Mime;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.SignInLogs;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.FeatureManagement;

namespace Microsoft.AspNetCore.Builder;

/// <summary>The sign in logs API.</summary>
public static class SignInLogApi
{
    /// <summary>Maps the sign in logs endpoints.</summary>
    /// <param name="builder">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static IEndpointRouteBuilder MapSignInLogs(this IEndpointRouteBuilder builder) {
        var options = builder.GetEndpointOptions<SignInLogOptions>();
        var group = builder.MapGroup($"{options.ApiPrefix}/")
                           .WithGroupName("identity")
                           .WithTags("SignInLogs")
                           .RequireAuthorization(policyBuilder =>
                                policyBuilder.AddAuthenticationSchemes("IdentityServerApiAccessToken")
                                             .RequireAdmin()
                                             .RequireClaim(BasicClaimTypes.Scope, options.ApiScope)
                           )
                           .ProducesProblem(StatusCodes.Status401Unauthorized)
                           .ProducesProblem(StatusCodes.Status403Forbidden);
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", options.ApiScope);
        // GET: /api/sign-in-logs
        group.MapGet("/sign-in-logs", async (
            [FromServices] ISignInLogStore signInLogStore,
            [FromServices] IFeatureManager featureManager,
            [AsParameters] ListOptions options,
            [AsParameters] SignInLogEntryFilter filter
        ) => {
            if (!await featureManager.IsEnabledAsync(IdentityServerFeatures.SignInLogs)) {
                return Results.NotFound();
            }
            var signInLogs = await signInLogStore.ListAsync(ListOptions.Create(options, filter));
            return TypedResults.Ok(signInLogs);
        })
        .Produces<ResultSet<SignInLogEntry>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
        .WithDescription("Gets the list of sign in logs produced by the Identity system.")
        .WithName("GetSignInLogs")
        .WithSummary("Gets the list of sign in logs produced by the Identity system.");
        // PATCH: /api/sign-in-logs/{rowId}
        group.MapPatch("/sign-in-logs/{rowId}", async (
            [FromServices] ISignInLogStore signInLogStore,
            [FromServices] IFeatureManager featureManager,
            [FromRoute] Guid rowId,
            [FromBody] SignInLogEntryRequest model
        ) => {
            if (!await featureManager.IsEnabledAsync(IdentityServerFeatures.SignInLogs)) {
                return Results.NotFound();
            }
            var rowsAffected = await signInLogStore.UpdateAsync(rowId, model);
            return rowsAffected == 0 ? Results.NotFound() : Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound, "application/problem+json")
        .WithDescription("Patches the specified log entry by updating the properties given in the request.")
        .WithName("PatchSignInLog")
        .WithSummary("Patches the specified log entry by updating the properties given in the request.");
        return group;
    }
}