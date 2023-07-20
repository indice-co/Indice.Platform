using System.Security.Claims;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.SignInLogs;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Http;
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
        var allowedScopes = new[] { 
            options.ApiScope, 
            IdentityEndpoints.SubScopes.Logs 
        }
        .Where(x => x is not null)
        .Cast<string>()
        .ToArray();
        var group = builder
            .MapGroup($"{options.ApiPrefix}/")
            .WithGroupName("identity")
            .WithTags("SignInLogs")
            .RequireAuthorization(policy => policy
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
            )
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        
        // GET: /api/sign-in-logs
        group.MapGet("sign-in-logs", async (
            ISignInLogStore signInLogStore,
            IFeatureManager featureManager,
            [AsParameters] ListOptions options,
            [AsParameters] SignInLogEntryFilter filter
        ) => {
            if (!await featureManager.IsEnabledAsync(IdentityServerFeatures.SignInLogs)) {
                return Results.NotFound();
            }
            var signInLogs = await signInLogStore.ListAsync(options, filter);
            return TypedResults.Ok(signInLogs);
        })
        .Produces<ResultSet<SignInLogEntry>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithName("GetSignInLogs")
        .WithSummary("Gets the list of sign in logs produced by the Identity system.")
        .RequireAuthorization(IdentityEndpoints.Policies.BeLogsReader);
        
        // GET: /api/my/sign-in-logs
        group.MapGet("my/sign-in-logs", async (
            ClaimsPrincipal currentUser,
            ISignInLogStore signInLogStore,
            IFeatureManager featureManager,
            [AsParameters] ListOptions options,
            [AsParameters] SignInLogEntryFilterBase filter
        ) => {
            if (!await featureManager.IsEnabledAsync(IdentityServerFeatures.SignInLogs)) {
                return Results.NotFound();
            }
            if (options.Size > 100) {
                return TypedResults.ValidationProblem(ValidationErrors.AddError("size", "Max allowed value for page size is 100."));
            }
            var signInLogs = await signInLogStore.ListAsync(options, new SignInLogEntryFilter {
                From = filter.From,
                To = filter.To,
                ApplicationId = filter.ApplicationId,
                SignInType = filter.SignInType,
                Subject = currentUser.FindSubjectId()
            });
            return TypedResults.Ok(signInLogs);
        })
        .Produces<ResultSet<SignInLogEntry>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithName("GetMySignInLogs")
        .WithSummary("Gets the list of sign in logs for the current user.");
        
        // PATCH: /api/sign-in-logs/{rowId}
        group.MapPatch("sign-in-logs/{rowId}", async (
            ISignInLogStore signInLogStore,
            IFeatureManager featureManager,
            Guid rowId,
            SignInLogEntryRequest model
        ) => {
            if (!await featureManager.IsEnabledAsync(IdentityServerFeatures.SignInLogs)) {
                return Results.NotFound();
            }
            var rowsAffected = await signInLogStore.UpdateAsync(rowId, model);
            return rowsAffected == 0 ? Results.NotFound() : Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithName("PatchSignInLog")
        .WithSummary("Patches the specified log entry by updating the properties given in the request.")
        .RequireAuthorization(IdentityEndpoints.Policies.BeLogsWriter);
        
        return group;
    }
}
