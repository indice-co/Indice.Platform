using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.EntityFramework.Entities;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity.Api.Filters;
using Indice.AspNetCore.Identity.Api.Models;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.AspNetCore.Identity.Data;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Api.Controllers
{
    /// <summary>
    /// Contains operations for managing application's identity and API resources.
    /// </summary>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal Server Error</response>
    [Route("api/resources")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Admin)]
    [ProblemDetailsExceptionFilter]
    internal class ResourcesController : ControllerBase
    {
        private readonly ExtendedConfigurationDbContext _configurationDbContext;
        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "Resources";

        /// <summary>
        /// Creates an instance of <see cref="ResourcesController"/>.
        /// </summary>
        /// <param name="configurationDbContext">Abstraction for the configuration context.</param>
        public ResourcesController(ExtendedConfigurationDbContext configurationDbContext) {
            _configurationDbContext = configurationDbContext ?? throw new ArgumentNullException(nameof(configurationDbContext));
        }

        /// <summary>
        /// Returns a list of <see cref="IdentityResourceInfo"/> objects containing the total number of identity resources in the database and the data filtered according to the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <param name="options">List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet("identity")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<IdentityResourceInfo>))]
        public async Task<IActionResult> GetIdentityResources([FromQuery] ListOptions options) {
            var query = _configurationDbContext.IdentityResources.AsNoTracking();
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
            return Ok(resources);
        }

        /// <summary>
        /// Gets an identity resource by it's unique id.
        /// </summary>
        /// <param name="resourceId">The identifier of the identity resource.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IdentityResourceInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [HttpGet("identity/{resourceId:int}")]
        [CacheResourceFilter]
        public async Task<IActionResult> GetIdentityResource([FromRoute] int resourceId) {
            var resource = await _configurationDbContext.IdentityResources
                                                        .Include(x => x.UserClaims)
                                                        .AsNoTracking()
                                                        .SingleOrDefaultAsync(x => x.Id == resourceId);
            if (resource == null) {
                return NotFound();
            }
            return Ok(new IdentityResourceInfo {
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

        /// <summary>
        /// Creates a new identity resource.
        /// </summary>
        /// <param name="request">Contains info about the identity resource to be created.</param>
        /// <response code="201">Created</response>
        [HttpPost("identity")]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(IdentityResourceInfo))]
        public async Task<IActionResult> CreateIdentityResource([FromBody] CreateResourceRequest request) {
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
            _configurationDbContext.IdentityResources.Add(resource);
            await _configurationDbContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetIdentityResource), Name, new { resourceId = resource.Id }, new IdentityResourceInfo {
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

        /// <summary>
        /// Updates an identity resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the identity resource.</param>
        /// <param name="request">Contains info about the identity resource to be updated.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPut("identity/{resourceId:int}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter]
        public async Task<IActionResult> UpdateIdentityResource([FromRoute] int resourceId, [FromBody] UpdateIdentityResourceRequest request) {
            var resource = await _configurationDbContext.IdentityResources.SingleOrDefaultAsync(x => x.Id == resourceId);
            if (resource == null) {
                return NotFound();
            }
            resource.DisplayName = request.DisplayName;
            resource.Description = request.Description;
            resource.Enabled = request.Enabled;
            resource.Emphasize = request.Emphasize;
            resource.Required = request.Required;
            resource.ShowInDiscoveryDocument = request.ShowInDiscoveryDocument;
            await _configurationDbContext.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Adds claims to an identity resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the identity resource.</param>
        /// <param name="claims">The claims to add.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPost("identity/{resourceId:int}/claims")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "identity/{resourceId}" })]
        public async Task<IActionResult> AddIdentityResourceClaims([FromRoute] int resourceId, [FromBody] string[] claims) {
            var resource = await _configurationDbContext.IdentityResources.SingleOrDefaultAsync(x => x.Id == resourceId);
            if (resource == null) {
                return NotFound();
            }
            resource.UserClaims = new List<IdentityResourceClaim>();
            resource.UserClaims.AddRange(claims.Select(x => new IdentityResourceClaim {
                IdentityResourceId = resourceId,
                Type = x
            }));
            await _configurationDbContext.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Removes a specified claim from an identity resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the identity resource.</param>
        /// <param name="claim">The of the claim to remove.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("identity/{resourceId:int}/claims/{claim}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "identity/{resourceId}" })]
        public async Task<IActionResult> DeleteIdentityResourceClaim([FromRoute] int resourceId, [FromRoute] string claim) {
            var resource = await _configurationDbContext.IdentityResources.Include(x => x.UserClaims).SingleOrDefaultAsync(x => x.Id == resourceId);
            if (resource == null) {
                return NotFound();
            }
            if (resource.UserClaims == null) {
                resource.UserClaims = new List<IdentityResourceClaim>();
            }
            var claimToRemove = resource.UserClaims.Select(x => x.Type == claim).ToList();
            if (claimToRemove?.Count == 0) {
                return NotFound();
            }
            resource.UserClaims.RemoveAll(x => x.Type == claim);
            await _configurationDbContext.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Permanently deletes an identity resource.
        /// </summary>
        /// <param name="resourceId">The id of the identity resource to delete.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("identity/{resourceId:int}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter]
        public async Task<IActionResult> DeleteIdentityResource([FromRoute] int resourceId) {
            var resource = await _configurationDbContext.IdentityResources.SingleOrDefaultAsync(x => x.Id == resourceId);
            if (resource == null) {
                return NotFound();
            }
            _configurationDbContext.IdentityResources.Remove(resource);
            await _configurationDbContext.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Returns a list of <see cref="ApiResourceInfo"/> objects containing the total number of API resources in the database and the data filtered according to the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <param name="options">List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet("protected")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<ApiResourceInfo>))]
        public async Task<IActionResult> GetApiResources([FromQuery] ListOptions options) {
            var query = _configurationDbContext.ApiResources.AsNoTracking();
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
            return Ok(resources);
        }

        /// <summary>
        /// Returns a list of <see cref="ApiScopeInfo"/> objects containing the total number of API scopes in the database and the data filtered according to the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <param name="options">List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet("protected/scopes")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<ApiScopeInfo>))]
        public async Task<IActionResult> GetApiScopes([FromQuery] ListOptions options) {
            var query = _configurationDbContext.ApiScopes.Include(x => x.Properties).AsNoTracking();
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
                Translations = GetTranslationsFromApiScope(apiScope)
            })
            .ToResultSetAsync(options);
            return Ok(scopes);
        }

        /// <summary>
        /// Gets an API resource by it's unique id.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ApiResourceInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [HttpGet("protected/{resourceId:int}")]
        [CacheResourceFilter]
        public async Task<IActionResult> GetApiResource([FromRoute] int resourceId) {
            var apiResource = await _configurationDbContext.ApiResources
                .AsNoTracking()
                .Select(x => new ApiResourceInfo {
                    Id = x.Id,
                    Name = x.Name,
                    DisplayName = x.DisplayName,
                    Description = x.Description,
                    Enabled = x.Enabled,
                    NonEditable = x.NonEditable,
                    AllowedClaims = x.UserClaims.Select(x => x.Type),
                    Scopes = x.Scopes.Any() ? x.Scopes.Join(
                        _configurationDbContext.ApiScopes.Include(y => y.Properties),
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
                        Translations = GetTranslationsFromApiScope(result.ApiScope)
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
                return NotFound();
            }
            return Ok(apiResource);
        }

        /// <summary>
        /// Creates a new API resource.
        /// </summary>
        /// <param name="request">Contains info about the API resource to be created.</param>
        /// <response code="201">Created</response>
        [HttpPost("protected")]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(ApiResourceInfo))]
        public async Task<IActionResult> CreateApiResource([FromBody] CreateResourceRequest request) {
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
            _configurationDbContext.ApiResources.Add(apiResource);
            var apiScope = new ApiScope {
                Name = request.Name,
                DisplayName = request.DisplayName,
                Description = request.Description,
                Enabled = true,
                ShowInDiscoveryDocument = true,
                UserClaims = request.UserClaims.Select(claim => new ApiScopeClaim { Type = claim }).ToList()
            };
            _configurationDbContext.ApiScopes.Add(apiScope);
            await _configurationDbContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetApiResource), Name, new { resourceId = apiResource.Id }, new ApiResourceInfo {
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
            });
        }

        /// <summary>
        /// Updates an API resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <param name="request">Contains info about the API resource to be updated.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPut("protected/{resourceId:int}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter]
        public async Task<IActionResult> UpdateApiResource([FromRoute] int resourceId, [FromBody] UpdateApiResourceRequest request) {
            var resource = await _configurationDbContext.ApiResources.SingleOrDefaultAsync(x => x.Id == resourceId);
            if (resource == null) {
                return NotFound();
            }
            resource.DisplayName = request.DisplayName;
            resource.Description = request.Description;
            resource.Enabled = request.Enabled;
            await _configurationDbContext.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Adds a new scope to an existing API resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <param name="request">Contains info about the API scope to be created.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPost("protected/{resourceId:int}/secrets")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SecretInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "protected/{resourceId}" })]
        public async Task<IActionResult> AddApiResourceSecret([FromRoute] int resourceId, [FromBody] CreateSecretRequest request) {
            var resource = await _configurationDbContext.ApiResources.SingleOrDefaultAsync(x => x.Id == resourceId);
            if (resource == null) {
                return NotFound();
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
            await _configurationDbContext.SaveChangesAsync();
            return Ok(new SecretInfo {
                Id = secretToAdd.Id,
                Description = secretToAdd.Description,
                Expiration = secretToAdd.Expiration,
                Type = secretToAdd.Type,
                Value = "*****"
            });
        }

        /// <summary>
        /// Removes a specified claim from an API resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <param name="secretId">The identifier of the API resource secret to remove.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("protected/{resourceId:int}/secrets/{secretId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "protected/{resourceId}" })]
        public async Task<IActionResult> DeleteApiResourceSecret([FromRoute] int resourceId, [FromRoute] int secretId) {
            var resource = await _configurationDbContext.ApiResources.Include(x => x.Secrets).SingleOrDefaultAsync(x => x.Id == resourceId);
            if (resource == null) {
                return NotFound();
            }
            if (resource.Secrets == null) {
                resource.Secrets = new List<ApiResourceSecret>();
            }
            var secretToRemove = resource.Secrets.SingleOrDefault(x => x.Id == secretId);
            if (secretToRemove == null) {
                return NotFound();
            }
            resource.Secrets.Remove(secretToRemove);
            await _configurationDbContext.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Adds claims to an API resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <param name="claims">The API or identity resources to add.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpPost("protected/{resourceId:int}/claims")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "protected/{resourceId}" })]
        public async Task<IActionResult> AddApiResourceClaims([FromRoute] int resourceId, [FromBody] string[] claims) {
            var resource = await _configurationDbContext.ApiResources.SingleOrDefaultAsync(x => x.Id == resourceId);
            if (resource == null) {
                return NotFound();
            }
            resource.UserClaims = new List<ApiResourceClaim>();
            resource.UserClaims.AddRange(claims.Select(x => new ApiResourceClaim {
                ApiResourceId = resourceId,
                Type = x
            }));
            await _configurationDbContext.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Removes a specified claim from an API resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <param name="claim">The identifier of the API resource claim to remove.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("protected/{resourceId:int}/claims/{claim}")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "protected/{resourceId}" })]
        public async Task<IActionResult> DeleteApiResourceClaim([FromRoute] int resourceId, [FromRoute] string claim) {
            var resource = await _configurationDbContext.ApiResources.Include(x => x.UserClaims).SingleOrDefaultAsync(x => x.Id == resourceId);
            if (resource == null) {
                return NotFound();
            }
            if (resource.UserClaims == null) {
                resource.UserClaims = new List<ApiResourceClaim>();
            }
            var claimToRemove = resource.UserClaims.Select(x => x.Type == claim).ToList();
            if (claimToRemove?.Count == 0) {
                return NotFound();
            }
            resource.UserClaims.RemoveAll(x => x.Type == claim);
            await _configurationDbContext.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Adds a new scope to an existing API resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <param name="request">Contains info about the API scope to be created.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="404">Not Found</response>
        [HttpPost("protected/{resourceId:int}/scopes")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ApiScopeInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "protected/{resourceId}" })]
        public async Task<IActionResult> AddApiResourceScope([FromRoute] int resourceId, [FromBody] CreateApiScopeRequest request) {
            var resource = await _configurationDbContext.ApiResources.Include(x => x.Scopes).SingleOrDefaultAsync(x => x.Id == resourceId);
            if (resource == null) {
                return NotFound();
            }
            var apiScope = await _configurationDbContext.ApiScopes.AsNoTracking().SingleOrDefaultAsync(apiScope => apiScope.Name == request.Name);
            if (apiScope != null) {
                ModelState.AddModelError(nameof(request.Name).ToLower(), $"There is already an API scope with name: {apiScope.Name}.");
                return BadRequest(new ValidationProblemDetails(ModelState));
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
            _configurationDbContext.ApiScopes.Add(apiScopeToAdd);
            await _configurationDbContext.SaveChangesAsync();
            return Ok(new ApiScopeInfo {
                Id = apiResourceScope.Id,
                Name = apiScopeToAdd.Name,
                DisplayName = apiScopeToAdd.DisplayName,
                Description = apiScopeToAdd.Description,
                UserClaims = apiScopeToAdd.UserClaims.Select(x => x.Type),
                Emphasize = apiScopeToAdd.Emphasize,
                ShowInDiscoveryDocument = apiScopeToAdd.ShowInDiscoveryDocument,
                Translations = GetTranslationsFromApiScope(apiScopeToAdd)
            });
        }

        /// <summary>
        /// Updates a specified scope of an API resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <param name="scopeId">The identifier of the API resource.</param>
        /// <param name="request">Contains info about the API scope to be updated.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpPut("protected/{resourceId:int}/scopes/{scopeId:int}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "protected/{resourceId}" })]
        public async Task<IActionResult> UpdateApiResourceScope([FromRoute] int resourceId, [FromRoute] int scopeId, [FromBody] UpdateApiScopeRequest request) {
            var apiResourceScope = await _configurationDbContext.ApiResources.AsNoTracking().Where(x => x.Id == resourceId).SelectMany(x => x.Scopes).SingleOrDefaultAsync(x => x.Id == scopeId);
            if (apiResourceScope == null) {
                return NotFound();
            }
            var apiScope = await _configurationDbContext.ApiScopes.Include(x => x.Properties).SingleOrDefaultAsync(x => x.Name == apiResourceScope.Scope);
            apiScope.DisplayName = request.DisplayName;
            apiScope.Description = request.Description;
            apiScope.Required = request.Required;
            apiScope.ShowInDiscoveryDocument = request.ShowInDiscoveryDocument;
            apiScope.Emphasize = request.Emphasize;
            var apiScoreTranslations = apiScope.Properties?.SingleOrDefault(x => x.Key == IdentityServerApi.ObjectTranslationKey);
            if (apiScoreTranslations == null) {
                AddTranslationsToApiScope(apiScope, request.Translations.ToJson());
            } else {
                apiScoreTranslations.Value = request.Translations.ToJson() ?? string.Empty;
            }
            await _configurationDbContext.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Deletes a specified scope from an API resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <param name="scopeId">The identifier of the API resource scope.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("protected/{resourceId:int}/scopes/{scopeId:int}")]
        [CacheResourceFilter(DependentPaths = new string[] { "protected/{resourceId}" })]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteApiResourceScope([FromRoute] int resourceId, [FromRoute] int scopeId) {
            var apiResource = await _configurationDbContext.ApiResources.Include(x => x.Scopes).SingleOrDefaultAsync(x => x.Id == resourceId);
            if (apiResource == null) {
                return NotFound();
            }
            var apiResourceScope = apiResource.Scopes?.SingleOrDefault(x => x.Id == scopeId);
            if (apiResourceScope == null) {
                return NotFound();
            }
            apiResource.Scopes.Remove(apiResourceScope);
            var apiScope = await _configurationDbContext.ApiScopes.SingleOrDefaultAsync(x => x.Name == apiResourceScope.Scope);
            if (apiScope == null) {
                return NotFound();
            }
            _configurationDbContext.ApiScopes.Remove(apiScope);
            await _configurationDbContext.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Adds claims to an API scope of a protected resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <param name="scopeId">The identifier of the API resource scope.</param>
        /// <param name="claims">The claims to add to the scope.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpPost("protected/{resourceId:int}/scopes/{scopeId:int}/claims")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "protected/{resourceId}" })]
        public async Task<IActionResult> AddApiResourceScopeClaims([FromRoute] int resourceId, [FromRoute] int scopeId, [FromBody] string[] claims) {
            var apiResourceScope = await _configurationDbContext
                .ApiResources
                .AsNoTracking()
                .Where(apiResource => apiResource.Id == resourceId)
                .SelectMany(apiResource => apiResource.Scopes)
                .SingleOrDefaultAsync(apiResourceScope => apiResourceScope.Id == scopeId);
            if (apiResourceScope == null) {
                return NotFound();
            }
            var apiScope = await _configurationDbContext.ApiScopes.Include(apiScope => apiScope.UserClaims).SingleOrDefaultAsync(apiScope => apiScope.Name == apiResourceScope.Scope);
            if (apiScope == null) {
                return NotFound();
            }
            apiScope.UserClaims.AddRange(claims.Select(claim => new ApiScopeClaim { ScopeId = apiScope.Id, Type = claim }));
            await _configurationDbContext.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Deletes a claim from an API scope of a protected resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <param name="scopeId">The identifier of the API resource scope.</param>
        /// <param name="claim">The claim to remove from the scope.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("protected/{resourceId:int}/scopes/{scopeId:int}/claims/{claim}")]
        [CacheResourceFilter(DependentPaths = new string[] { "protected/{resourceId}" })]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteApiResourceScopeClaim([FromRoute] int resourceId, [FromRoute] int scopeId, [FromRoute] string claim) {
            var apiResourceScope = await _configurationDbContext
                .ApiResources
                .Where(apiResource => apiResource.Id == resourceId)
                .SelectMany(apiResource => apiResource.Scopes)
                .SingleOrDefaultAsync(apiResourceScope => apiResourceScope.Id == scopeId);
            if (apiResourceScope == null) {
                return NotFound();
            }
            var apiScope = await _configurationDbContext.ApiScopes.Include(x => x.UserClaims).SingleOrDefaultAsync(apiScope => apiScope.Name == apiResourceScope.Scope);
            if (apiScope == null) {
                return NotFound();
            }
            var claimToRemove = apiScope.UserClaims.SingleOrDefault(apiScopeClaim => apiScopeClaim.Type == claim);
            if (claimToRemove == null) {
                return NotFound();
            }
            apiScope.UserClaims.Remove(claimToRemove);
            await _configurationDbContext.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Permanently deletes an API resource.
        /// </summary>
        /// <param name="resourceId">The id of the API resource to delete.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("protected/{resourceId:int}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter]
        public async Task<IActionResult> DeleteApiResource([FromRoute] int resourceId) {
            var apiResource = await _configurationDbContext.ApiResources.Include(x => x.Scopes).SingleOrDefaultAsync(x => x.Id == resourceId);
            if (apiResource == null) {
                return NotFound();
            }
            _configurationDbContext.ApiResources.Remove(apiResource);
            var apiScopes = await _configurationDbContext.ApiScopes.Where(apiScope => apiResource.Scopes.Select(apiResourceScope => apiResourceScope.Scope).Contains(apiScope.Name)).ToListAsync();
            if (apiScopes.Any()) {
                _configurationDbContext.ApiScopes.RemoveRange(apiScopes);
            }
            await _configurationDbContext.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Adds translations to an <see cref="ApiScope"/>.
        /// </summary>
        /// <remarks>If the parameter translations is null, string.Empty will be persisted.</remarks>
        /// <param name="apiScope">The <see cref="ApiScope"/>.</param>
        /// <param name="translations">The JSON string with the translations</param>
        private void AddTranslationsToApiScope(ApiScope apiScope, string translations) {
            apiScope.Properties ??= new List<ApiScopeProperty>();
            apiScope.Properties.Add(new ApiScopeProperty {
                Key = IdentityServerApi.ObjectTranslationKey,
                Value = translations ?? string.Empty,
                Scope = apiScope
            });
        }

        /// <summary>
        /// Deserialize the JSON translation of an <see cref="ApiScope"/>.
        /// </summary>
        /// <param name="apiScope">The API scope.</param>
        private static TranslationDictionary<ApiScopeTranslation> GetTranslationsFromApiScope(ApiScope apiScope) => 
            TranslationDictionary<ApiScopeTranslation>.FromJson(apiScope.Properties.Any(x => x.Key == IdentityServerApi.ObjectTranslationKey)
                ? apiScope.Properties.Single(x => x.Key == IdentityServerApi.ObjectTranslationKey).Value
                : string.Empty
            );
    }
}
