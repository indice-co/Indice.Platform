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

/// <summary>Contains operations for managing Resources.</summary>
public static class ResourcesApi
{
    /// <summary>
    /// Adds enpoints for various lookups.
    /// </summary>
    /// <param name="routes"></param>
    /// <returns></returns>
    public static RouteGroupBuilder MapManageResources(this IdentityServerEndpointRouteBuilder routes) {
        var options = routes.GetEndpointOptions();
        var group = routes.MapGroup($"{options.ApiPrefix}/resources");
        group.WithTags("Resources");
        group.WithGroupName("identity");
        // Add security requirements, all incoming requests to this API *must*
        // be authenticated with a valid user.
        var allowedScopes = new[] { options.ApiScope, IdentityEndpoints.SubScopes.Clients }.Where(x => x != null).ToArray();
        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme));
        
             
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized);
        
        group.MapGet("identity", ResourceHandlers.GetIdentityResources)
             .WithName(nameof(ResourceHandlers.GetIdentityResources))
             .WithSummary("Returns a list of IdentityResourceInfo objects containing the total number of identity resources in the database and the data filtered according to the provided ListOptions.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsReader);

        group.MapGet("identity/{resourceId:int}", ResourceHandlers.GetIdentityResource)
             .WithName(nameof(ResourceHandlers.GetIdentityResource))
             .WithSummary("Gets an identity resource by it's unique id.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsReader)
             .CacheOutputMemory();

        group.MapPost("identity/{resourceId:int}", ResourceHandlers.CreateIdentityResource)
             .WithName(nameof(ResourceHandlers.CreateIdentityResource))
             .WithSummary("Creates a new identity resource.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .WithParameterValidation<CreateResourceRequest>();

        group.MapPut("identity/{resourceId:int}", ResourceHandlers.UpdateIdentityResource)
             .WithName(nameof(ResourceHandlers.UpdateIdentityResource))
             .WithSummary("Updates an identity resource.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ResourceHandlers.GetIdentityResource))
             .WithParameterValidation<UpdateIdentityResourceRequest>();

        group.MapPost("identity/{resourceId:int}/claims", ResourceHandlers.AddIdentityResourceClaims)
             .WithName(nameof(ResourceHandlers.AddIdentityResourceClaims))
             .WithSummary("Adds claims to an identity resource.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ResourceHandlers.GetIdentityResource));

        group.MapDelete("identity/{resourceId:int}/claims/{claim}", ResourceHandlers.DeleteIdentityResourceClaim)
             .WithName(nameof(ResourceHandlers.DeleteIdentityResourceClaim))
             .WithSummary("Removes a specified claim from an identity resource.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ResourceHandlers.GetIdentityResource));

        group.MapDelete("identity/{resourceId:int}", ResourceHandlers.DeleteIdentityResource)
             .WithName(nameof(ResourceHandlers.DeleteIdentityResource))
             .WithSummary("Permanently deletes an identity resource.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ResourceHandlers.GetIdentityResource));

        group.MapGet("protected", ResourceHandlers.GetApiResources)
             .WithName(nameof(ResourceHandlers.GetApiResources))
             .WithSummary("Returns a list of ApiResourceInfo objects containing the total number of API resources in the database and the data filtered according to the provided ListOptions.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsReader);

        group.MapGet("protected/scopes", ResourceHandlers.GetApiScopes)
             .WithName(nameof(ResourceHandlers.GetApiScopes))
             .WithSummary("Returns a list of ApiResourceInfo objects containing the total number of API resources in the database and the data filtered according to the provided ListOptions.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsReader);

        group.MapGet("protected/{resourceId:int}", ResourceHandlers.GetApiResource)
             .WithName(nameof(ResourceHandlers.GetApiResource))
             .WithSummary("Gets an API resource by it's unique id.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsReader);

        group.MapPost("protected", ResourceHandlers.CreateApiResource)
             .WithName(nameof(ResourceHandlers.CreateApiResource))
             .WithSummary("Creates a new API resource.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .WithParameterValidation<CreateResourceRequest>();

        group.MapPut("protected/{resourceId:int}", ResourceHandlers.UpdateApiResource)
             .WithName(nameof(ResourceHandlers.UpdateApiResource))
             .WithSummary("Updates an API resource.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ResourceHandlers.GetApiResource))
             .WithParameterValidation<UpdateApiResourceRequest>();

        group.MapPost("protected/{resourceId:int}/secrets", ResourceHandlers.AddApiResourceSecret)
             .WithName(nameof(ResourceHandlers.AddApiResourceSecret))
             .WithSummary("Adds a new scope to an existing API resource.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ResourceHandlers.GetApiResource))
             .WithParameterValidation<CreateSecretRequest>();

        group.MapDelete("protected/{resourceId:int}/secrets/{secretId}", ResourceHandlers.DeleteApiResourceSecret)
             .WithName(nameof(ResourceHandlers.DeleteApiResourceSecret))
             .WithSummary("Removes a specified claim from an API resource.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ResourceHandlers.GetApiResource));

        group.MapPost("protected/{resourceId:int}/claims", ResourceHandlers.AddApiResourceClaims)
             .WithName(nameof(ResourceHandlers.AddApiResourceClaims))
             .WithSummary("Adds claims to an API resource.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ResourceHandlers.GetApiResource));

        group.MapDelete("protected/{resourceId:int}/claims/{claim}", ResourceHandlers.DeleteApiResourceClaim)
             .WithName(nameof(ResourceHandlers.DeleteApiResourceClaim))
             .WithSummary("Removes a specified claim from an API resource.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ResourceHandlers.GetApiResource));

        group.MapPost("protected/{resourceId:int}/scopes", ResourceHandlers.AddApiResourceScope)
             .WithName(nameof(ResourceHandlers.AddApiResourceScope))
             .WithSummary("Adds a new scope to an existing API resource.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ResourceHandlers.GetApiResource))
             .WithParameterValidation<CreateApiScopeRequest>();

        group.MapPut("protected/{resourceId:int}/scopes/{scopeId:int}", ResourceHandlers.UpdateApiResourceScope)
             .WithName(nameof(ResourceHandlers.UpdateApiResourceScope))
             .WithSummary("Updates a specified scope of an API resource.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ResourceHandlers.GetApiResource))
             .WithParameterValidation<UpdateApiScopeRequest>();

        group.MapDelete("protected/{resourceId:int}/scopes/{scopeId:int}", ResourceHandlers.DeleteApiResourceScope)
             .WithName(nameof(ResourceHandlers.DeleteApiResourceScope))
             .WithSummary("Deletes a specified scope from an API resource.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ResourceHandlers.GetApiResource));

        group.MapPost("protected/{resourceId:int}/scopes/{scopeId:int}/claims", ResourceHandlers.AddApiResourceScopeClaims)
             .WithName(nameof(ResourceHandlers.AddApiResourceScopeClaims))
             .WithSummary("Adds claims to an API scope of a protected resource.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ResourceHandlers.GetApiResource));

        group.MapDelete("protected/{resourceId:int}/scopes/{scopeId:int}/claims/{claim}", ResourceHandlers.DeleteApiResourceScopeClaim)
             .WithName(nameof(ResourceHandlers.DeleteApiResourceScopeClaim))
             .WithSummary("Deletes a claim from an API scope of a protected resource.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ResourceHandlers.GetApiResource));

        group.MapDelete("protected/{resourceId:int}", ResourceHandlers.DeleteApiResource)
             .WithName(nameof(ResourceHandlers.DeleteApiResource))
             .WithSummary("Permanently deletes an API resource.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeClientsWriter)
             .InvalidateCache(nameof(ResourceHandlers.GetApiResource));
        return group;
    }
}
