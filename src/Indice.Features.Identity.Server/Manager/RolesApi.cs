using IdentityModel;
using Indice.AspNetCore.Http.Filters;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Manager;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Contains operations for managing application roles.</summary>
public static class RolesApi
{
    internal const string CacheTagPrefix = "Roles";

    /// <summary>Adds endpoints for managing application roles.</summary>
    /// <param name="routes">Indice Identity Server route builder.</param>
    public static RouteGroupBuilder MapManageRoles(this IdentityServerEndpointRouteBuilder routes) {

        var options = routes.GetEndpointOptions();
        var group = routes.MapGroup($"{options.ApiPrefix}/roles");
        group.WithTags("Roles");
        group.WithGroupName("identity");
        // Add security requirements, all incoming requests to this API *must* be authenticated with a valid user.
        var allowedScopes = new[] { options.ApiScope, IdentityEndpoints.SubScopes.Users }.Where(x => x != null).Cast<string>().ToArray();
        group.RequireAuthorization(policy => policy
             .RequireAuthenticatedUser()
             .AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
             .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
        );



        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("", RoleHandlers.GetRoles)
             .WithName(nameof(RoleHandlers.GetRoles))
             .WithSummary($"Returns a list of {nameof(RoleInfo)} objects containing the total number of roles in the database and the data filtered according to the provided ListOptions.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersReader)
             .CacheOutput(policy => policy.NoCache());

        group.MapGet("{roleId}", RoleHandlers.GetRole)
             .WithName(nameof(RoleHandlers.GetRole))
             .WithSummary("Gets a role by it's unique id.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersReader)
             .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(60))
                                          .SetAutoTag()
                                          .SetAuthorized()
                                          .SetVaryByRouteValue(["roleId"]))
             .WithCacheTag(CacheTagPrefix, ["roleId"])
             .CacheAuthorized();

        group.MapPost("", RoleHandlers.CreateRole)
             .WithName(nameof(RoleHandlers.CreateRole))
             .WithSummary("Creates a new role.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter)
             .InvalidateCacheTag(DashboardApi.CacheTagPrefix, [], [JwtClaimTypes.Subject])
             .WithParameterValidation<CreateRoleRequest>();

        group.MapPut("{roleId}", RoleHandlers.UpdateRole)
             .WithName(nameof(RoleHandlers.UpdateRole))
             .WithSummary("Updates an existing role.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter)
             .InvalidateCacheTag(CacheTagPrefix, ["roleId"], []) //added
             .WithParameterValidation<UpdateRoleRequest>();

        group.MapDelete("{roleId}", RoleHandlers.DeleteRole)
             .WithName(nameof(RoleHandlers.DeleteRole))
             .WithSummary("Permanently deletes an existing role.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter);

        return group;
    }
}
