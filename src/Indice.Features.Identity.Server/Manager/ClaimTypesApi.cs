using Indice.AspNetCore.Http.Filters;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Manager;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Contains operations for managing application claim types.</summary>
public static class ClaimTypesApi
{
    /// <summary>
    /// Add Identity ClaimType Endpoints
    /// </summary>
    /// <param name="routes"></param>
    /// <returns></returns>
    public static RouteGroupBuilder MapClaimTypes(this IdentityServerEndpointRouteBuilder routes) {
        var options = routes.GetEndpointOptions();
        var group = routes.MapGroup($"{options.ApiPrefix}/claim-types");
        group.WithTags("ClaimTypes");
        group.WithGroupName("identity");
        // Add security requirements, all incoming requests to this API *must*
        // be authenticated with a valid user.
        var allowedScopes = new[] { options.ApiScope, IdentityEndpoints.SubScopes.Users }.Where(x => x != null).ToArray();
        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .CacheOutputMemory();

        group.MapGet("", ClaimTypeHandlers.GetClaimTypes)
             .WithName(nameof(ClaimTypeHandlers.GetClaimTypes))
             .WithSummary($"Returns a list of {nameof(ClaimTypeInfo)} objects containing the total number of claim types in the database and the data filtered according to the provided ListOptions.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersReader)
             .NoCache();

        group.MapGet("{claimTypeId}", ClaimTypeHandlers.GetClaimType)
             .WithName(nameof(ClaimTypeHandlers.GetClaimType))
             .WithSummary("Gets a claim type by it's unique id.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersReader);

        group.MapPost("", ClaimTypeHandlers.CreateClaimType)
             .WithName(nameof(ClaimTypeHandlers.CreateClaimType))
             .WithSummary("Creates a new claim type.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter);

        group.MapPut("{claimTypeId}", ClaimTypeHandlers.UpdateClaimType)
             .WithName(nameof(ClaimTypeHandlers.UpdateClaimType))
             .WithSummary("Updates an existing claim type.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter)
             .InvalidateCache(nameof(ClaimTypeHandlers.GetClaimType));

        group.MapDelete("{claimTypeId}", ClaimTypeHandlers.DeleteClaimType)
             .WithName(nameof(ClaimTypeHandlers.DeleteClaimType))
             .WithSummary("Permanently deletes an existing claim type.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter)
             .InvalidateCache(nameof(ClaimTypeHandlers.GetClaimType));

        return group;
    }
}
