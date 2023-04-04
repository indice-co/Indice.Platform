using IdentityModel;
using Indice.AspNetCore.Http.Filters;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Manager;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Contains operations for managing application claim types.</summary>
public static class DashboardApi
{
    /// <summary>
    /// Add Identity ClaimType Endpoints
    /// </summary>
    /// <param name="routes"></param>
    /// <returns></returns>
    public static RouteGroupBuilder MapDashboard(this IdentityServerEndpointRouteBuilder routes) {
        var options = routes.GetEndpointOptions();
        var group = routes.MapGroup($"{options.ApiPrefix}/dashboard");
        group.WithTags("Dashboard");
        group.WithGroupName("identity");
        // Add security requirements, all incoming requests to this API *must*
        // be authenticated with a valid user.
        var allowedScopes = new[] { options.ApiScope, IdentityEndpoints.SubScopes.Users, IdentityEndpoints.SubScopes.Clients }.Where(x => x != null).ToArray();
        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));
        group.WithOpenApi();
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("news", DashboardHandlers.GetNews)
             .WithName(nameof(DashboardHandlers.GetNews))
             .WithSummary("Displays blog posts from the official IdentityServer blog.")
             .AllowAnonymous()
             .CacheOutputMemory(expiration: 3600);
             //.CacheOutput(policy => {
             //    policy.SetVaryByQuery("page", "size");
             //});

        group.MapGet("summary", DashboardHandlers.GetSystemSummary)
             .WithName(nameof(DashboardHandlers.GetSystemSummary))
             .WithSummary("Gets some useful information as a summary of the system.")
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes)
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersOrClientsReader)
             .CacheOutputMemory(expiration: 5, varyByClaimType: new string[] { JwtClaimTypes.Subject });

        group.MapGet("ui", DashboardHandlers.GetUiFeatures)
             .WithName(nameof(DashboardHandlers.GetUiFeatures))
             .WithSummary("Gets the UI features status.")
             .AllowAnonymous()
             .CacheOutputMemory(expiration: 120);
        return group;
    }
}
