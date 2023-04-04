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

/// <summary>Contains operations for managing lookups.</summary>
public static class LookupsApi
{
    /// <summary>
    /// Adds enpoints for various lookups.
    /// </summary>
    /// <param name="routes"></param>
    /// <returns></returns>
    public static RouteGroupBuilder MapLookups(this IdentityServerEndpointRouteBuilder routes) {
        var options = routes.GetEndpointOptions();
        var group = routes.MapGroup($"{options.ApiPrefix}/lookups");
        group.WithTags("Lookups");
        group.WithGroupName("identity");
        // Add security requirements, all incoming requests to this API *must*
        // be authenticated with a valid user.
        var allowedScopes = new[] { options.ApiScope }.Where(x => x != null).ToArray();
        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme));
        
             
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("external-providers", LookupHandlers.GetExternalProviders)
             .WithName(nameof(LookupHandlers.GetExternalProviders))
             .WithSummary("Gets the list of available external providers.");

        return group;
    }
}
