﻿using Indice.AspNetCore.Http.Filters;
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
public static class ClaimTypesApi
{
    internal const string CacheTagPrefix = "claimTypes";
    /// <summary>Maps the endpoints for managing application claim types.</summary>
    /// <param name="routes">Indice Identity Server route builder.</param>
    public static RouteGroupBuilder MapManageClaimTypes(this IdentityServerEndpointRouteBuilder routes) {

        var options = routes.GetEndpointOptions();
        var group = routes.MapGroup($"{options.ApiPrefix}/claim-types");
        group.WithTags("ClaimTypes");
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

        group.MapGet(string.Empty, ClaimTypeHandlers.GetClaimTypes)
             .WithName(nameof(ClaimTypeHandlers.GetClaimTypes))
             .WithSummary($"Returns a list of {nameof(ClaimTypeInfo)} objects containing the total number of claim types in the database and the data filtered according to the provided ListOptions.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersReader)
             .CacheOutput(policy => policy.NoCache());

        group.MapGet("{claimTypeId}", ClaimTypeHandlers.GetClaimType)
             .WithName(nameof(ClaimTypeHandlers.GetClaimType))
             .WithSummary("Gets a claim type by it's unique id.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersReader)
             .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(60))
                                          .SetAutoTag()
                                          .SetAuthorized()
                                          .SetVaryByRouteValue(["claimTypeId"]))
             .WithCacheTag(CacheTagPrefix, ["claimTypeId"])
             .CacheAuthorized();

        group.MapPost("", ClaimTypeHandlers.CreateClaimType)
             .WithName(nameof(ClaimTypeHandlers.CreateClaimType))
             .WithSummary("Creates a new claim type.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter)
             .WithParameterValidation<CreateClaimTypeRequest>();

        group.MapPut("{claimTypeId}", ClaimTypeHandlers.UpdateClaimType)
             .WithName(nameof(ClaimTypeHandlers.UpdateClaimType))
             .WithSummary("Updates an existing claim type.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter)
             .InvalidateCacheTag(CacheTagPrefix, ["claimTypeId"], [])
             .WithParameterValidation<UpdateClaimTypeRequest>();

        group.MapDelete("{claimTypeId}", ClaimTypeHandlers.DeleteClaimType)
             .WithName(nameof(ClaimTypeHandlers.DeleteClaimType))
             .WithSummary("Permanently deletes an existing claim type.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter)
             .InvalidateCacheTag(CacheTagPrefix, ["claimTypeId"], []);

        return group;
    }
}
