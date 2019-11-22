using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.EntityFramework.Entities;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Features
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
    internal class ResourceController : ControllerBase
    {
        private readonly ExtendedConfigurationDbContext _configurationDbContext;
        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "Resources";

        /// <summary>
        /// Creates an instance of <see cref="ResourceController"/>.
        /// </summary>
        /// <param name="configurationDbContext">Abstraction for the configuration context.</param>
        public ResourceController(ExtendedConfigurationDbContext configurationDbContext) {
            _configurationDbContext = configurationDbContext ?? throw new ArgumentNullException(nameof(configurationDbContext));
        }

        /// <summary>
        /// Returns a list of <see cref="IdentityResourceInfo"/> objects containing the total number of identity resources in the database and the data filtered according to the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <param name="options">List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet("identity")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<IdentityResourceInfo>))]
        public async Task<ActionResult<ResultSet<IdentityResourceInfo>>> GetIdentityResources([FromQuery]ListOptions options) {
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
        public async Task<ActionResult<IdentityResourceInfo>> GetIdentityResource([FromRoute]int resourceId) {
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
        public async Task<ActionResult<IdentityResourceInfo>> CreateIdentityResource([FromBody]CreateResourceRequest request) {
            var resource = new IdentityResource {
                Name = request.Name,
                DisplayName = request.DisplayName,
                Description = request.Description,
                Enabled = true,
                ShowInDiscoveryDocument = true,
                UserClaims = request.UserClaims.Select(x => new IdentityClaim {
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
        public async Task<IActionResult> UpdateIdentityResource([FromRoute]int resourceId, [FromBody]UpdateIdentityResourceRequest request) {
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
        /// Permanently deletes an identity resource.
        /// </summary>
        /// <param name="resourceId">The id of the identity resource to delete.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("identity/{resourceId:int}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter]
        public async Task<IActionResult> DeleteIdentityResource([FromRoute]int resourceId) {
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
        public async Task<ActionResult<ResultSet<ApiResourceInfo>>> GetApiResources([FromQuery]ListOptions options) {
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
        /// Returns a list of <see cref="ScopeInfo"/> objects containing the total number of API scopes in the database and the data filtered according to the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <param name="options">List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet("protected/scopes")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<ScopeInfo>))]
        public async Task<ActionResult<ResultSet<ScopeInfo>>> GetApiScopes([FromQuery]ListOptions options) {
            var query = _configurationDbContext.ApiResources.AsNoTracking();
            if (!string.IsNullOrEmpty(options.Search)) {
                var searchTerm = options.Search.ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(searchTerm) || x.Description.ToLower().Contains(searchTerm));
            }
            var scopes = await query.SelectMany(x => x.Scopes).Select(x => new ScopeInfo {
                Id = x.Id,
                Name = x.Name,
                DisplayName = x.DisplayName,
                Description = x.Description,
                Emphasize = x.Emphasize,
                Required = x.Required,
                ShowInDiscoveryDocument = x.ShowInDiscoveryDocument,
                UserClaims = x.UserClaims.Select(x => x.Type)
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
        public async Task<ActionResult<ApiResourceInfo>> GetApiResource([FromRoute]int resourceId) {
            var resource = await _configurationDbContext.ApiResources
                                                        .Include(x => x.UserClaims)
                                                        .Include(x => x.Secrets)
                                                        .Include(x => x.Scopes)
                                                        .ThenInclude(x => x.UserClaims)
                                                        .AsNoTracking()
                                                        .SingleOrDefaultAsync(x => x.Id == resourceId);
            if (resource == null) {
                return NotFound();
            }
            return Ok(new ApiResourceInfo {
                Id = resource.Id,
                Name = resource.Name,
                DisplayName = resource.DisplayName,
                Description = resource.Description,
                Enabled = resource.Enabled,
                NonEditable = resource.NonEditable,
                AllowedClaims = resource.UserClaims.Select(x => x.Type),
                Scopes = resource.Scopes.Any() ? resource.Scopes.Select(x => new ScopeInfo {
                    Id = x.Id,
                    Name = x.Name,
                    DisplayName = x.DisplayName,
                    Description = x.Description,
                    Required = x.Required,
                    Emphasize = x.Emphasize,
                    ShowInDiscoveryDocument = x.ShowInDiscoveryDocument,
                    UserClaims = x.UserClaims.Any() ? x.UserClaims.Select(x => x.Type) : default
                }) : default,
                Secrets = resource.Secrets.Any() ? resource.Secrets.Select(x => new ApiSecretInfo {
                    Id = x.Id,
                    Type = x.Type,
                    Value = "*****",
                    Description = x.Description,
                    Expiration = x.Expiration
                }) : default
            });
        }

        /// <summary>
        /// Creates a new API resource.
        /// </summary>
        /// <param name="request">Contains info about the API resource to be created.</param>
        /// <response code="201">Created</response>
        [HttpPost("protected")]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(ApiResourceInfo))]
        public async Task<ActionResult<ApiResourceInfo>> CreateApiResource([FromBody]CreateResourceRequest request) {
            var resource = new ApiResource {
                Name = request.Name,
                DisplayName = request.DisplayName,
                Description = request.Description,
                Enabled = true,
                UserClaims = request.UserClaims.Select(x => new ApiResourceClaim {
                    Type = x
                })
                .ToList(),
                Scopes = new List<ApiScope> {
                    new ApiScope {
                        Name = request.Name,
                        DisplayName = request.DisplayName,
                        Description = request.Description,
                        ShowInDiscoveryDocument = true
                    }
                }
            };
            _configurationDbContext.ApiResources.Add(resource);
            await _configurationDbContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetApiResource), Name, new { resourceId = resource.Id }, new ApiResourceInfo {
                Id = resource.Id,
                Name = resource.Name,
                DisplayName = resource.DisplayName,
                Description = resource.Description,
                Enabled = resource.Enabled,
                NonEditable = resource.NonEditable,
                AllowedClaims = resource.UserClaims.Select(x => x.Type),
                Scopes = resource.Scopes.Select(x => new ScopeInfo {
                    Id = x.Id,
                    Name = x.Name,
                    DisplayName = x.DisplayName,
                    Description = x.Description,
                    ShowInDiscoveryDocument = x.ShowInDiscoveryDocument
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
        public async Task<IActionResult> UpdateApiResource([FromRoute]int resourceId, [FromBody]UpdateApiResourceRequest request) {
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
        /// <response code="201">Created</response>
        /// <response code="404">Not Found</response>
        [HttpPost("protected/{resourceId:int}/secrets")]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(SecretInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(dependentPaths: new string[] { "protected/{resourceId}" })]
        public async Task<ActionResult<SecretInfo>> AddApiResourceSecret([FromRoute]int resourceId, [FromBody]CreateSecretRequest request) {
            var resource = await _configurationDbContext.ApiResources.SingleOrDefaultAsync(x => x.Id == resourceId);
            if (resource == null) {
                return NotFound();
            }
            var secretToAdd = new ApiSecret {
                Description = request.Description,
                Value = request.Value.ToSha256(),
                Expiration = request.Expiration,
                Type = $"{request.Type}"
            };
            resource.Secrets = new List<ApiSecret> {
                secretToAdd
            };
            await _configurationDbContext.SaveChangesAsync();
            return CreatedAtAction(string.Empty, new SecretInfo {
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
        [CacheResourceFilter(dependentPaths: new string[] { "protected/{resourceId}" })]
        public async Task<ActionResult> DeleteApiResourceSecret([FromRoute]int resourceId, [FromRoute]int secretId) {
            var resource = await _configurationDbContext.ApiResources.Include(x => x.Secrets).SingleOrDefaultAsync(x => x.Id == resourceId);
            if (resource == null) {
                return NotFound();
            }
            if (resource.Secrets == null) {
                resource.Secrets = new List<ApiSecret>();
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
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPost("protected/{resourceId:int}/claims")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(dependentPaths: new string[] { "protected/{resourceId}" })]
        public async Task<ActionResult> AddApiResourceClaims([FromRoute]int resourceId, [FromBody]string[] claims) {
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
            return Ok();
        }

        /// <summary>
        /// Removes a specified claim from an API resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <param name="claim">The identifier of the API resource claim to remove.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("protected/{resourceId:int}/claims/{claim}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(dependentPaths: new string[] { "protected/{resourceId}" })]
        public async Task<ActionResult> DeleteApiResourceClaim([FromRoute]int resourceId, [FromRoute]string claim) {
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
            return Ok();
        }

        /// <summary>
        /// Adds a new scope to an existing API resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <param name="request">Contains info about the API scope to be created.</param>
        /// <response code="201">Created</response>
        /// <response code="404">Not Found</response>
        [HttpPost("protected/{resourceId:int}/scopes")]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(ScopeInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(dependentPaths: new string[] { "protected/{resourceId}" })]
        public async Task<ActionResult<ScopeInfo>> AddApiResourceScope([FromRoute]int resourceId, [FromBody]CreateApiScopeRequest request) {
            var resource = await _configurationDbContext.ApiResources.SingleOrDefaultAsync(x => x.Id == resourceId);
            if (resource == null) {
                return NotFound();
            }
            var scopeToAdd = new ApiScope {
                Name = request.Name,
                DisplayName = request.DisplayName,
                Description = request.Description,
                Required = request.Required,
                Emphasize = request.Emphasize,
                ShowInDiscoveryDocument = request.ShowInDiscoveryDocument,
                UserClaims = request.UserClaims.Select(x => new ApiScopeClaim {
                    Type = x
                })
                .ToList()
            };
            resource.Scopes = new List<ApiScope> {
                scopeToAdd
            };
            await _configurationDbContext.SaveChangesAsync();
            return CreatedAtAction(string.Empty, new ScopeInfo {
                Id = scopeToAdd.Id,
                Name = scopeToAdd.Name,
                DisplayName = scopeToAdd.DisplayName,
                Description = scopeToAdd.Description,
                UserClaims = scopeToAdd.UserClaims.Select(x => x.Type),
                Emphasize = scopeToAdd.Emphasize,
                Required = scopeToAdd.Required,
                ShowInDiscoveryDocument = scopeToAdd.ShowInDiscoveryDocument
            });
        }

        /// <summary>
        /// Updates a specified scope of an API resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <param name="scopeId">The identifier of the API resource.</param>
        /// <param name="request">Contains info about the API scope to be updated.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPut("protected/{resourceId:int}/scopes/{scopeId:int}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(dependentPaths: new string[] { "protected/{resourceId}" })]
        public async Task<IActionResult> UpdateApiResourceScope([FromRoute]int resourceId, [FromRoute]int scopeId, [FromBody]UpdateApiScopeRequest request) {
            var scope = await _configurationDbContext.ApiResources
                                                     .Where(x => x.Id == resourceId)
                                                     .SelectMany(x => x.Scopes)
                                                     .SingleOrDefaultAsync(x => x.Id == scopeId);
            if (scope == null) {
                return NotFound();
            }
            scope.DisplayName = request.DisplayName;
            scope.Description = request.Description;
            scope.Emphasize = request.Emphasize;
            scope.Required = request.Required;
            scope.ShowInDiscoveryDocument = request.ShowInDiscoveryDocument;
            await _configurationDbContext.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Deletes a specified scope from an API resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <param name="scopeId">The identifier of the API resource scope.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("protected/{resourceId:int}/scopes/{scopeId:int}")]
        [CacheResourceFilter(dependentPaths: new string[] { "protected/{resourceId}" })]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(ScopeInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<ActionResult> DeleteApiResourceScope([FromRoute]int resourceId, [FromRoute]int scopeId) {
            var resource = await _configurationDbContext.ApiResources.Include(x => x.Scopes).SingleOrDefaultAsync(x => x.Id == resourceId);
            if (resource == null) {
                return NotFound();
            }
            if (resource.Scopes == null) {
                resource.Scopes = new List<ApiScope>();
            }
            var scopeToRemove = resource.Scopes.SingleOrDefault(x => x.Id == scopeId);
            if (scopeToRemove == null) {
                return NotFound();
            }
            resource.Scopes.Remove(scopeToRemove);
            await _configurationDbContext.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Adds claims to an API scope of a protected resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <param name="scopeId">The identifier of the API resource scope.</param>
        /// <param name="claims">The claims to add to the scope.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPost("protected/{resourceId:int}/scopes/{scopeId:int}/claims")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(dependentPaths: new string[] { "protected/{resourceId}" })]
        public async Task<ActionResult> AddApiResourceScopeClaims([FromRoute]int resourceId, [FromRoute]int scopeId, [FromBody]string[] claims) {
            var resourceScope = await _configurationDbContext.ApiResources
                                                             .Where(x => x.Id == resourceId)
                                                             .SelectMany(x => x.Scopes)
                                                             .Where(x => x.Id == scopeId)
                                                             .SingleOrDefaultAsync();
            if (resourceScope == null) {
                return NotFound();
            }
            resourceScope.UserClaims = new List<ApiScopeClaim>();
            resourceScope.UserClaims.AddRange(claims.Select(x => new ApiScopeClaim {
                ApiScopeId = scopeId,
                Type = x
            }));
            await _configurationDbContext.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Deletes a claim from an API scope of a protected resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <param name="scopeId">The identifier of the API resource scope.</param>
        /// <param name="claim">The claim to remove from the scope.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("protected/{resourceId:int}/scopes/{scopeId:int}/claims/{claim}")]
        [CacheResourceFilter(dependentPaths: new string[] { "protected/{resourceId}" })]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<ActionResult> DeleteApiResourceScopeClaim([FromRoute]int resourceId, [FromRoute]int scopeId, [FromRoute]string claim) {
            var scope = await _configurationDbContext.ApiResources
                                                     .Include(x => x.Scopes)
                                                     .ThenInclude(x => x.UserClaims)
                                                     .Where(x => x.Id == resourceId)
                                                     .SelectMany(x => x.Scopes)
                                                     .SingleOrDefaultAsync(x => x.Id == scopeId);
            if (scope == null) {
                return NotFound();
            }
            var claimToRemove = scope.UserClaims.SingleOrDefault(x => x.Type == claim);
            if (claimToRemove == null) {
                return NotFound();
            }
            scope.UserClaims.Remove(claimToRemove);
            await _configurationDbContext.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Permanently deletes an API resource.
        /// </summary>
        /// <param name="resourceId">The id of the API resource to delete.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("protected/{resourceId:int}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter]
        public async Task<IActionResult> DeleteApiResource([FromRoute]int resourceId) {
            var resource = await _configurationDbContext.ApiResources.SingleOrDefaultAsync(x => x.Id == resourceId);
            if (resource == null) {
                return NotFound();
            }
            _configurationDbContext.ApiResources.Remove(resource);
            await _configurationDbContext.SaveChangesAsync();
            return Ok();
        }
    }
}
