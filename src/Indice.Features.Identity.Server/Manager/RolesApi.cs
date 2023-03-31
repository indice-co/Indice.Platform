using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Manager;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Contains operations for managing application roles.</summary>
public static class RolesApi
{
    /// <summary>
    /// Add Identity ClaimType Endpoints
    /// </summary>
    /// <param name="routes"></param>
    /// <returns></returns>
    public static RouteGroupBuilder MapRoles(this IdentityServerEndpointRouteBuilder routes) {
        var options = routes.GetEndpointOptions();
        var group = routes.MapGroup($"{options.ApiPrefix}/roles");
        group.WithTags("Roles");
        group.WithGroupName("identity");
        // Add security requirements, all incoming requests to this API *must*
        // be authenticated with a valid user.
        var allowedScopes = new[] { options.ApiScope, IdentityEndpoints.SubScopes.Users}.Where(x => x != null).ToArray();
        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("", RoleHandlers.GetRoles)
             .WithName(nameof(RoleHandlers.GetRoles))
             .WithSummary($"Returns a list of {nameof(RoleInfo)} objects containing the total number of claim types in the database and the data filtered according to the provided ListOptions.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersReader);

        group.MapGet("{roleId}", RoleHandlers.GetRole)
             .WithName(nameof(RoleHandlers.GetRole))
             .WithSummary("Gets a claim type by it's unique id.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersReader);

        group.MapPost("", RoleHandlers.CreateRole)
             .WithName(nameof(RoleHandlers.CreateRole))
             .WithSummary("Creates a new claim type.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter);

        group.MapPut("{roleId}", RoleHandlers.UpdateRole)
             .WithName(nameof(RoleHandlers.UpdateRole))
             .WithSummary("Updates an existing claim type.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter);

        group.MapDelete("{roleId}", RoleHandlers.DeleteRole)
             .WithName(nameof(RoleHandlers.DeleteRole))
             .WithSummary("Permanently deletes an existing claim type.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter);

        return group;
    }
}
