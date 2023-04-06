using IdentityModel;
using IdentityServer4;
using IdentityServer4.EntityFramework.Entities;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Identity.Server.Manager;

internal static class ResourceHandlers
{
    internal static async Task<Ok<ResultSet<IdentityResourceInfo>>> GetIdentityResources(
        ExtendedConfigurationDbContext configurationDbContext,
        [AsParameters] ListOptions options) {
        var query = configurationDbContext.IdentityResources.AsNoTracking();
        if (!string.IsNullOrEmpty(options.Search)) {
            var searchTerm = options.Search.ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(searchTerm) || x.Description.Contains(searchTerm));
        }
        var resources = await query.Select(resource => new IdentityResourceInfo {
            Id = resource.Id,
            Name = resource.Name,
            DisplayName = resource.DisplayName,
            Description = resource.Description,
            Enabled = resource.Enabled,
            Required = resource.Required,
            Emphasize = resource.Emphasize,
            NonEditable = resource.NonEditable,
            ShowInDiscoveryDocument = resource.ShowInDiscoveryDocument,
            AllowedClaims = resource.UserClaims.Select(claim => claim.Type)
        })
        .ToResultSetAsync(options);
        return TypedResults.Ok(resources);
    }

    internal static async Task<Results<Ok<IdentityResourceInfo>, NotFound>> GetIdentityResource(
        ExtendedConfigurationDbContext configurationDbContext,
        int resourceId) {
        var resource = await configurationDbContext.IdentityResources.Include(x => x.UserClaims).AsNoTracking().SingleOrDefaultAsync(x => x.Id == resourceId);
        if (resource == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(new IdentityResourceInfo {
            Id = resource.Id,
            Name = resource.Name,
            DisplayName = resource.DisplayName,
            Description = resource.Description,
            Enabled = resource.Enabled,
            Required = resource.Required,
            Emphasize = resource.Emphasize,
            NonEditable = resource.NonEditable,
            ShowInDiscoveryDocument = resource.ShowInDiscoveryDocument,
            AllowedClaims = resource.UserClaims.Select(x => x.Type)
        });
    }

    internal static async Task<CreatedAtRoute<IdentityResourceInfo>> CreateIdentityResource(
        ExtendedConfigurationDbContext configurationDbContext,
        CreateResourceRequest request) {
        var resource = new IdentityResource {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            Enabled = true,
            ShowInDiscoveryDocument = true,
            UserClaims = request.UserClaims.Select(x => new IdentityResourceClaim {
                Type = x
            })
            .ToList()
        };
        configurationDbContext.IdentityResources.Add(resource);
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.CreatedAtRoute(new IdentityResourceInfo {
            Id = resource.Id,
            Name = resource.Name,
            DisplayName = resource.DisplayName,
            Description = resource.Description,
            Enabled = resource.Enabled,
            Required = resource.Required,
            Emphasize = resource.Emphasize,
            NonEditable = resource.NonEditable,
            ShowInDiscoveryDocument = resource.ShowInDiscoveryDocument,
            AllowedClaims = resource.UserClaims.Select(x => x.Type)
        }, nameof(GetIdentityResource), new { resourceId = resource.Id });
    }

    internal static async Task<Results<NoContent, NotFound>> UpdateIdentityResource(
        ExtendedConfigurationDbContext configurationDbContext,
        int resourceId, UpdateIdentityResourceRequest request) {
        var resource = await configurationDbContext.IdentityResources.SingleOrDefaultAsync(x => x.Id == resourceId);
        if (resource == null) {
            return TypedResults.NotFound();
        }
        resource.DisplayName = request.DisplayName;
        resource.Description = request.Description;
        resource.Enabled = request.Enabled;
        resource.Emphasize = request.Emphasize;
        resource.Required = request.Required;
        resource.ShowInDiscoveryDocument = request.ShowInDiscoveryDocument;
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> AddIdentityResourceClaims(
        ExtendedConfigurationDbContext configurationDbContext,
        int resourceId, string[] claims) {
        var resource = await configurationDbContext.IdentityResources.SingleOrDefaultAsync(x => x.Id == resourceId);
        if (resource == null) {
            return TypedResults.NotFound();
        }
        if (!(claims?.Length > 0)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError("claims", "A claims list is required"));
        }
        resource.UserClaims = new List<IdentityResourceClaim>();
        resource.UserClaims.AddRange(claims.Select(x => new IdentityResourceClaim {
            IdentityResourceId = resourceId,
            Type = x
        }));
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound>> DeleteIdentityResourceClaim(
        ExtendedConfigurationDbContext configurationDbContext,
        int resourceId, string claim) {
        var resource = await configurationDbContext.IdentityResources.Include(x => x.UserClaims).SingleOrDefaultAsync(x => x.Id == resourceId);
        if (resource == null) {
            return TypedResults.NotFound();
        }
        if (resource.UserClaims == null) {
            resource.UserClaims = new List<IdentityResourceClaim>();
        }
        var claimToRemove = resource.UserClaims.Select(x => x.Type == claim).ToList();
        if (claimToRemove?.Count == 0) {
            return TypedResults.NotFound();
        }
        resource.UserClaims.RemoveAll(x => x.Type == claim);
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound>> DeleteIdentityResource(
        ExtendedConfigurationDbContext configurationDbContext,
        int resourceId
    ) {
        var resource = await configurationDbContext.IdentityResources.SingleOrDefaultAsync(x => x.Id == resourceId);
        if (resource == null) {
            return TypedResults.NotFound();
        }
        configurationDbContext.IdentityResources.Remove(resource);
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<Ok<ResultSet<ApiResourceInfo>>> GetApiResources(
        ExtendedConfigurationDbContext configurationDbContext,
        [AsParameters] ListOptions options
    ) {
        var query = configurationDbContext.ApiResources.AsNoTracking();
        if (!string.IsNullOrEmpty(options.Search)) {
            var searchTerm = options.Search.ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(searchTerm) || x.Description.ToLower().Contains(searchTerm));
        }
        var resources = await query.Select(x => new ApiResourceInfo {
            Id = x.Id,
            Name = x.Name,
            DisplayName = x.DisplayName,
            Description = x.Description,
            Enabled = x.Enabled,
            NonEditable = x.NonEditable,
            AllowedClaims = x.UserClaims.Select(x => x.Type)
        })
        .ToResultSetAsync(options);
        return TypedResults.Ok(resources);
    }

    internal static async Task<Ok<ResultSet<ApiScopeInfo>>> GetApiScopes(
        ExtendedConfigurationDbContext configurationDbContext,
        [AsParameters] ListOptions options
    ) {
        var query = configurationDbContext.ApiScopes.Include(x => x.Properties).AsNoTracking();
        if (!string.IsNullOrEmpty(options.Search)) {
            var searchTerm = options.Search.ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(searchTerm) || x.Description.ToLower().Contains(searchTerm));
        }
        var scopes = await query.Select(apiScope => new ApiScopeInfo {
            Id = apiScope.Id,
            Name = apiScope.Name,
            Description = apiScope.Description,
            DisplayName = apiScope.DisplayName,
            Emphasize = apiScope.Emphasize,
            UserClaims = apiScope.UserClaims.Select(apiScopeClaim => apiScopeClaim.Type),
            ShowInDiscoveryDocument = apiScope.ShowInDiscoveryDocument,
            Translations = TranslationDictionary<ApiScopeTranslation>.FromJson(apiScope.Properties.Any(x => x.Key == ClientPropertyKeys.Translation)
                ? apiScope.Properties.Single(x => x.Key == ClientPropertyKeys.Translation).Value
                : string.Empty
            )
        })
        .ToResultSetAsync(options);
        return TypedResults.Ok(scopes);
    }

    internal static async Task<Results<Ok<ApiResourceInfo>, NotFound>> GetApiResource(
        ExtendedConfigurationDbContext configurationDbContext,
        int resourceId
    ) {
        var apiResource = await configurationDbContext.ApiResources
            .AsNoTracking()
            .Include(x => x.Properties)
            .Include(x => x.Scopes)
            .Select(x => new ApiResourceInfo {
                Id = x.Id,
                Name = x.Name,
                DisplayName = x.DisplayName,
                Description = x.Description,
                Enabled = x.Enabled,
                NonEditable = x.NonEditable,
                AllowedClaims = x.UserClaims.Select(x => x.Type),
                Scopes = x.Scopes.Any() ? x.Scopes.Join(
                    configurationDbContext.ApiScopes.Include(y => y.Properties),
                    apiResourceScope => apiResourceScope.Scope,
                    apiScope => apiScope.Name,
                    (apiResourceScope, apiScope) => new {
                        ApiResourceScope = apiResourceScope,
                        ApiScope = apiScope
                    }
                )
                .Select(result => new ApiScopeInfo {
                    Id = result.ApiResourceScope.Id,
                    Name = result.ApiScope.Name,
                    Description = result.ApiScope.Description,
                    DisplayName = result.ApiScope.DisplayName,
                    Emphasize = result.ApiScope.Emphasize,
                    UserClaims = result.ApiScope.UserClaims.Select(apiScopeClaim => apiScopeClaim.Type),
                    ShowInDiscoveryDocument = result.ApiScope.ShowInDiscoveryDocument,
                    Translations = TranslationDictionary<ApiScopeTranslation>.FromJson(result.ApiScope.Properties.Any(x => x.Key == ClientPropertyKeys.Translation)
                        ? result.ApiScope.Properties.Single(x => x.Key == ClientPropertyKeys.Translation).Value
                        : string.Empty
                    )
                }) : default,
                Secrets = x.Secrets.Any() ? x.Secrets.Select(x => new ApiSecretInfo {
                    Id = x.Id,
                    Type = x.Type,
                    Value = "*****",
                    Description = x.Description,
                    Expiration = x.Expiration
                }) : default
            })
            .SingleOrDefaultAsync(x => x.Id == resourceId);
        if (apiResource == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(apiResource);
    }

    internal static async Task<CreatedAtRoute<ApiResourceInfo>> CreateApiResource(
        ExtendedConfigurationDbContext configurationDbContext,
        CreateResourceRequest request
    ) {
        var apiResource = new ApiResource {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            Enabled = true,
            ShowInDiscoveryDocument = true,
            AllowedAccessTokenSigningAlgorithms = request.AllowedAccessTokenSigningAlgorithms,
            UserClaims = request.UserClaims.Select(claim => new ApiResourceClaim { Type = claim }).ToList(),
            Scopes = new List<ApiResourceScope> { new ApiResourceScope { Scope = request.Name } }
        };
        configurationDbContext.ApiResources.Add(apiResource);
        var apiScope = new ApiScope {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            Enabled = true,
            ShowInDiscoveryDocument = true,
            UserClaims = request.UserClaims.Select(claim => new ApiScopeClaim { Type = claim }).ToList()
        };
        configurationDbContext.ApiScopes.Add(apiScope);
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.CreatedAtRoute(new ApiResourceInfo {
            Id = apiResource.Id,
            Name = apiResource.Name,
            DisplayName = apiResource.DisplayName,
            Description = apiResource.Description,
            Enabled = apiResource.Enabled,
            NonEditable = apiResource.NonEditable,
            AllowedClaims = apiResource.UserClaims.Select(x => x.Type),
            Scopes = apiResource.Scopes.Select(x => new ApiScopeInfo {
                Id = apiScope.Id,
                Name = apiScope.Name,
                DisplayName = apiScope.DisplayName,
                Description = apiScope.Description,
                ShowInDiscoveryDocument = apiScope.ShowInDiscoveryDocument,
                Emphasize = apiScope.Emphasize,
                UserClaims = apiScope.UserClaims.Select(x => x.Type)
            })
        }, nameof(GetApiResource), new { resourceId = apiResource.Id });
    }

    internal static async Task<Results<NoContent, NotFound>> UpdateApiResource(
        ExtendedConfigurationDbContext configurationDbContext,
        int resourceId, 
        UpdateApiResourceRequest request
    ) {
        var resource = await configurationDbContext.ApiResources.SingleOrDefaultAsync(x => x.Id == resourceId);
        if (resource == null) {
            return TypedResults.NotFound();
        }
        resource.DisplayName = request.DisplayName;
        resource.Description = request.Description;
        resource.Enabled = request.Enabled;
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<Results<Ok<SecretInfo>, NotFound>> AddApiResourceSecret(
        ExtendedConfigurationDbContext configurationDbContext,
        int resourceId, 
        CreateSecretRequest request
    ) {
        var resource = await configurationDbContext.ApiResources.SingleOrDefaultAsync(x => x.Id == resourceId);
        if (resource == null) {
            return TypedResults.NotFound();
        }
        var secretToAdd = new ApiResourceSecret {
            Description = request.Description,
            Value = request.Value.ToSha256(),
            Expiration = request.Expiration,
            Type = IdentityServerConstants.SecretTypes.SharedSecret
        };
        resource.Secrets = new List<ApiResourceSecret> {
            secretToAdd
        };
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.Ok(new SecretInfo {
            Id = secretToAdd.Id,
            Description = secretToAdd.Description,
            Expiration = secretToAdd.Expiration,
            Type = secretToAdd.Type,
            Value = "*****"
        });
    }

    internal static async Task<Results<NoContent, NotFound>> DeleteApiResourceSecret(
        ExtendedConfigurationDbContext configurationDbContext,
        int resourceId, 
        int secretId
    ) {
        var resource = await configurationDbContext.ApiResources.Include(x => x.Secrets).SingleOrDefaultAsync(x => x.Id == resourceId);
        if (resource == null) {
            return TypedResults.NotFound();
        }
        if (resource.Secrets == null) {
            resource.Secrets = new List<ApiResourceSecret>();
        }
        var secretToRemove = resource.Secrets.SingleOrDefault(x => x.Id == secretId);
        if (secretToRemove == null) {
            return TypedResults.NotFound();
        }
        resource.Secrets.Remove(secretToRemove);
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> AddApiResourceClaims(
        ExtendedConfigurationDbContext configurationDbContext,
        int resourceId,
        string[] claims
    ) {
        var resource = await configurationDbContext.ApiResources.SingleOrDefaultAsync(x => x.Id == resourceId);
        if (resource == null) {
            return TypedResults.NotFound();
        }
        if (!(claims?.Length > 0)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError("claims", "A claims list is required"));
        }
        resource.UserClaims = new List<ApiResourceClaim>();
        resource.UserClaims.AddRange(claims.Select(x => new ApiResourceClaim {
            ApiResourceId = resourceId,
            Type = x
        }));
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound>> DeleteApiResourceClaim(
        ExtendedConfigurationDbContext configurationDbContext,
        int resourceId,
        string claim
    ) {
        var resource = await configurationDbContext.ApiResources.Include(x => x.UserClaims).SingleOrDefaultAsync(x => x.Id == resourceId);
        if (resource == null) {
            return TypedResults.NotFound();
        }
        if (resource.UserClaims == null) {
            resource.UserClaims = new List<ApiResourceClaim>();
        }
        var claimToRemove = resource.UserClaims.Select(x => x.Type == claim).ToList();
        if (claimToRemove?.Count == 0) {
            return TypedResults.NotFound();
        }
        resource.UserClaims.RemoveAll(x => x.Type == claim);
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<Results<Ok<ApiScopeInfo>, NotFound, ValidationProblem>> AddApiResourceScope(
        ExtendedConfigurationDbContext configurationDbContext,
        int resourceId,
        CreateApiScopeRequest request
    ) {
        var resource = await configurationDbContext.ApiResources.Include(x => x.Scopes).SingleOrDefaultAsync(x => x.Id == resourceId);
        if (resource == null) {
            return TypedResults.NotFound();
        }
        var apiScope = await configurationDbContext.ApiScopes.AsNoTracking().SingleOrDefaultAsync(apiScope => apiScope.Name == request.Name);
        if (apiScope != null) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(request.Name).ToLower(), $"There is already an API scope with name: {apiScope.Name}."));
        }
        var apiResourceScope = new ApiResourceScope {
            Scope = request.Name,
            ApiResourceId = resource.Id
        };
        resource.Scopes.Add(apiResourceScope);
        var apiScopeToAdd = new ApiScope {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            Enabled = true,
            Emphasize = request.Emphasize,
            ShowInDiscoveryDocument = request.ShowInDiscoveryDocument,
            Required = request.Required,
            UserClaims = request.UserClaims.Select(claim => new ApiScopeClaim { Type = claim }).ToList(),
            Properties = new List<ApiScopeProperty>()
        };
        if (request.Translations.Any()) {
            AddTranslationsToApiScope(apiScopeToAdd, request.Translations.ToJson());
        }
        configurationDbContext.ApiScopes.Add(apiScopeToAdd);
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.Ok(new ApiScopeInfo {
            Id = apiResourceScope.Id,
            Name = apiScopeToAdd.Name,
            DisplayName = apiScopeToAdd.DisplayName,
            Description = apiScopeToAdd.Description,
            UserClaims = apiScopeToAdd.UserClaims.Select(x => x.Type),
            Emphasize = apiScopeToAdd.Emphasize,
            ShowInDiscoveryDocument = apiScopeToAdd.ShowInDiscoveryDocument,
            Translations = TranslationDictionary<ApiScopeTranslation>.FromJson(apiScope.Properties.Any(x => x.Key == ClientPropertyKeys.Translation)
                ? apiScope.Properties.Single(x => x.Key == ClientPropertyKeys.Translation).Value
                : string.Empty
            )
        });
    }

    internal static async Task<Results<NoContent, NotFound>> UpdateApiResourceScope(
        ExtendedConfigurationDbContext configurationDbContext,
        int resourceId,
        int scopeId,
        UpdateApiScopeRequest request
    ) {
        var apiResourceScope = await configurationDbContext.ApiResources.AsNoTracking().Where(x => x.Id == resourceId).SelectMany(x => x.Scopes).SingleOrDefaultAsync(x => x.Id == scopeId);
        if (apiResourceScope == null) {
            return TypedResults.NotFound();
        }
        var apiScope = await configurationDbContext.ApiScopes.Include(x => x.Properties).SingleOrDefaultAsync(x => x.Name == apiResourceScope.Scope);
        apiScope.DisplayName = request.DisplayName;
        apiScope.Description = request.Description;
        apiScope.Required = request.Required;
        apiScope.ShowInDiscoveryDocument = request.ShowInDiscoveryDocument;
        apiScope.Emphasize = request.Emphasize;
        var apiScoreTranslations = apiScope.Properties?.SingleOrDefault(x => x.Key == ClientPropertyKeys.Translation);
        if (apiScoreTranslations == null) {
            AddTranslationsToApiScope(apiScope, request.Translations.ToJson());
        } else {
            apiScoreTranslations.Value = request.Translations.ToJson() ?? string.Empty;
        }
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound>> DeleteApiResourceScope(
        ExtendedConfigurationDbContext configurationDbContext,
        int resourceId, int scopeId) {
        var apiResource = await configurationDbContext.ApiResources.Include(x => x.Scopes).SingleOrDefaultAsync(x => x.Id == resourceId);
        if (apiResource == null) {
            return TypedResults.NotFound();
        }
        var apiResourceScope = apiResource.Scopes?.SingleOrDefault(x => x.Id == scopeId);
        if (apiResourceScope == null) {
            return TypedResults.NotFound();
        }
        apiResource.Scopes.Remove(apiResourceScope);
        var apiScope = await configurationDbContext.ApiScopes.SingleOrDefaultAsync(x => x.Name == apiResourceScope.Scope);
        if (apiScope == null) {
            return TypedResults.NotFound();
        }
        configurationDbContext.ApiScopes.Remove(apiScope);
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> AddApiResourceScopeClaims(
        ExtendedConfigurationDbContext configurationDbContext,
        int resourceId,
        int scopeId,
        string[] claims
    ) {
        var apiResourceScope = await configurationDbContext
            .ApiResources
            .AsNoTracking()
            .Where(apiResource => apiResource.Id == resourceId)
            .SelectMany(apiResource => apiResource.Scopes)
            .SingleOrDefaultAsync(apiResourceScope => apiResourceScope.Id == scopeId);
        if (apiResourceScope == null) {
            return TypedResults.NotFound();
        }
        if (!(claims?.Length > 0)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError("claims", "A claims list is required"));
        }
        var apiScope = await configurationDbContext.ApiScopes.Include(apiScope => apiScope.UserClaims).SingleOrDefaultAsync(apiScope => apiScope.Name == apiResourceScope.Scope);
        if (apiScope == null) {
            return TypedResults.NotFound();
        }
        apiScope.UserClaims.AddRange(claims.Select(claim => new ApiScopeClaim { ScopeId = apiScope.Id, Type = claim }));
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound>> DeleteApiResourceScopeClaim(
        ExtendedConfigurationDbContext configurationDbContext,
        int resourceId,
        int scopeId,
        string claim
    ) {
        var apiResourceScope = await configurationDbContext
             .ApiResources
             .Where(apiResource => apiResource.Id == resourceId)
             .SelectMany(apiResource => apiResource.Scopes)
             .SingleOrDefaultAsync(apiResourceScope => apiResourceScope.Id == scopeId);
        if (apiResourceScope == null) {
            return TypedResults.NotFound();
        }
        var apiScope = await configurationDbContext.ApiScopes.Include(x => x.UserClaims).SingleOrDefaultAsync(apiScope => apiScope.Name == apiResourceScope.Scope);
        if (apiScope == null) {
            return TypedResults.NotFound();
        }
        var claimToRemove = apiScope.UserClaims.SingleOrDefault(apiScopeClaim => apiScopeClaim.Type == claim);
        if (claimToRemove == null) {
            return TypedResults.NotFound();
        }
        apiScope.UserClaims.Remove(claimToRemove);
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound>> DeleteApiResource(
        ExtendedConfigurationDbContext configurationDbContext,
        int resourceId) {
        var apiResource = await configurationDbContext.ApiResources.Include(x => x.Scopes).SingleOrDefaultAsync(x => x.Id == resourceId);
        if (apiResource == null) {
            return TypedResults.NotFound();
        }
        configurationDbContext.ApiResources.Remove(apiResource);
        var apiScopes = await configurationDbContext.ApiScopes.Where(apiScope => apiResource.Scopes.Select(apiResourceScope => apiResourceScope.Scope).Contains(apiScope.Name)).ToListAsync();
        if (apiScopes.Any()) {
            configurationDbContext.ApiScopes.RemoveRange(apiScopes);
        }
        await configurationDbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    private static void AddTranslationsToApiScope(ApiScope apiScope, string translations) {
        apiScope.Properties ??= new List<ApiScopeProperty>();
        apiScope.Properties.Add(new ApiScopeProperty {
            Key = ClientPropertyKeys.Translation,
            Value = translations ?? string.Empty,
            Scope = apiScope
        });
    }
}
