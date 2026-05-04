using System.Security.Claims;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.SignInLogs;
using Indice.Features.Identity.SignInLogs;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>The sign in logs API.</summary>
public static class SignInLogApi
{
    /// <summary>Maps the sign in logs endpoints.</summary>
    /// <param name="builder">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static IEndpointRouteBuilder MapSignInLogs(this IEndpointRouteBuilder builder) {
        var isFeatureRegistered = builder.ServiceProvider.GetService<SignInLogEntryQueue>() is not null;
        var options = builder.GetEndpointOptions<SignInLogOptions>();
        if (!isFeatureRegistered || !options.Enable) {
            return builder;
        }
        var allowedScopes = new[] { options.ApiScope, IdentityEndpoints.SubScopes.Logs }.FilterOutNulls().ToArray();
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
        group.WithOpenApiSecurityRequirement("oauth2", allowedScopes);

        //Sign In Logs
        // GET: /api/sign-in-logs
        group.MapGet("sign-in-logs", SignInLogHandlers.GetSignInLogs)
             .WithName(nameof(SignInLogHandlers.GetSignInLogs))
             .WithSummary("Gets the list of sign in logs produced by the Identity system.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeLogsReader);

        // GET: /api/my/sign-in-logs
        group.MapGet("my/sign-in-logs", SignInLogHandlers.GetMySignInLogs)
             .WithName(nameof(SignInLogHandlers.GetMySignInLogs))
             .WithSummary("Gets the list of sign in logs for the current user.");

        // PATCH: /api/sign-in-logs/{rowId}
        group.MapPatch("sign-in-logs/{rowId}", SignInLogHandlers.PatchSignInLog)
             .WithName(nameof(SignInLogHandlers.PatchSignInLog))
             .WithSummary("Patches the specified log entry by updating the properties given in the request.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeLogsWriter);

        // GET: /api/sign-in-logs/locations
        group.MapGet("sign-in-logs/locations", SignInLogHandlers.GetSignInLocations)
             .WithName(nameof(SignInLogHandlers.GetSignInLocations))
             .WithSummary("Gets aggregates for sign-ins per city and country.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeLogsReader)
             .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(30)).SetAuthorized());

        return group;
    }
}
