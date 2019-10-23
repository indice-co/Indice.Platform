using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
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
    [GenericControllerNameConvention]
    [Route("api/resources")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Admin)]
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
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet("identity")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<IdentityResourceInfo>))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<ActionResult<ResultSet<IdentityResourceInfo>>> GetIdentityResources([FromQuery]ListOptions options) {
            var query = _configurationDbContext.IdentityResources.AsNoTracking();
            if (!string.IsNullOrEmpty(options.Search)) {
                var searchTerm = options.Search.ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(searchTerm) || x.Description.Contains(searchTerm));
            }
            var identityResources = await query.Select(resource => new IdentityResourceInfo {
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
            return Ok(identityResources);
        }

        /// <summary>
        /// Gets an identity resource by it's unique id.
        /// </summary>
        /// <param name="resourceId">The identifier of the identity resource.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IdentityResourceInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [HttpGet("identity/{resourceId:int}")]
        public async Task<ActionResult<IdentityResourceInfo>> GetIdentityResource([FromRoute]int resourceId) {
            var identityResource = await _configurationDbContext.IdentityResources
                                                                .Include(x => x.UserClaims)
                                                                .AsNoTracking()
                                                                .SingleOrDefaultAsync(x => x.Id == resourceId);
            if (identityResource == null) {
                return NotFound();
            }
            return Ok(new IdentityResourceInfo {
                Id = identityResource.Id,
                Name = identityResource.Name,
                DisplayName = identityResource.DisplayName,
                Description = identityResource.Description,
                Enabled = identityResource.Enabled,
                Required = identityResource.Required,
                Emphasize = identityResource.Emphasize,
                NonEditable = identityResource.NonEditable,
                ShowInDiscoveryDocument = identityResource.ShowInDiscoveryDocument,
                AllowedClaims = identityResource.UserClaims.Select(x => x.Type)
            });
        }

        /// <summary>
        /// Creates a new identity resource.
        /// </summary>
        /// <param name="request">Contains info about the identity resource to be created.</param>
        /// <response code="201">Created</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("identity")]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(IdentityResourceInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
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
        /// Permanently deletes an identity resource.
        /// </summary>
        /// <param name="resourceId">The id of the identity resource to delete.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [HttpDelete("identity/{resourceId:int}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
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
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet("protected")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<ApiResourceInfo>))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<ActionResult<ResultSet<ApiResourceInfo>>> GetProtectedResources([FromQuery]ListOptions options) {
            var query = _configurationDbContext.ApiResources.AsNoTracking();
            if (!string.IsNullOrEmpty(options.Search)) {
                var searchTerm = options.Search.ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(searchTerm) || x.Description.ToLower().Contains(searchTerm));
            }
            var apiResources = await query.Select(resource => new ApiResourceInfo {
                Id = resource.Id,
                Name = resource.Name,
                DisplayName = resource.DisplayName,
                Description = resource.Description,
                Enabled = resource.Enabled,
                NonEditable = resource.NonEditable,
                AllowedClaims = resource.UserClaims.Select(e => e.Type)
            })
            .ToResultSetAsync(options);
            return Ok(apiResources);
        }

        /// <summary>
        /// Gets an API resource by it's unique id.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ApiResourceInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [HttpGet("protected/{resourceId:int}")]
        public async Task<ActionResult<ApiResourceInfo>> GetApiResource([FromRoute]int resourceId) {
            var apiResource = await _configurationDbContext.ApiResources
                                                           .Include(x => x.UserClaims)
                                                           .Include(x => x.Secrets)
                                                           .Include(x => x.Scopes)
                                                           .ThenInclude(x => x.UserClaims)
                                                           .AsNoTracking()
                                                           .SingleOrDefaultAsync(x => x.Id == resourceId);
            if (apiResource == null) {
                return NotFound();
            }
            return Ok(new ApiResourceInfo {
                Id = apiResource.Id,
                Name = apiResource.Name,
                DisplayName = apiResource.DisplayName,
                Description = apiResource.Description,
                Enabled = apiResource.Enabled,
                NonEditable = apiResource.NonEditable,
                AllowedClaims = apiResource.UserClaims.Select(x => x.Type),
                Scopes = apiResource.Scopes.Any() ? apiResource.Scopes.Select(x => new ScopeInfo {
                    Id = x.Id,
                    Name = x.Name,
                    DisplayName = x.DisplayName,
                    Description = x.Description,
                    Required = x.Required,
                    Emphasize = x.Emphasize,
                    ShowInDiscoveryDocument = x.ShowInDiscoveryDocument,
                    UserClaims = x.UserClaims.Any() ? x.UserClaims.Select(x => x.Type) : default
                }) : default,
                Secrets = apiResource.Secrets.Any() ? apiResource.Secrets.Select(x => new ApiSecretInfo {
                    Id = x.Id,
                    Type = x.Type == nameof(SecretType.SharedSecret) ? SecretType.SharedSecret : SecretType.X509Thumbprint,
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
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("protected")]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(ApiResourceInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<ActionResult<ApiResourceInfo>> CreateProtectedResource([FromBody]CreateResourceRequest request) {
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
        /// Adds claims to an API resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <param name="claims">The API or identity resources to add.</param>
        /// <response code="200">Ok</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("protected/{resourceId:int}/claims")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<ActionResult> AddProtectedResourceClaims([FromRoute]int resourceId, [FromBody]string[] claims) {
            var apiResource = await _configurationDbContext.ApiResources.SingleOrDefaultAsync(x => x.Id == resourceId);
            if (apiResource == null) {
                return NotFound();
            }
            if (apiResource.UserClaims == null) {
                apiResource.UserClaims = new List<ApiResourceClaim>();
            }
            apiResource.UserClaims.AddRange(claims.Select(x => new ApiResourceClaim {
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
        /// <response code="200">Ok</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpDelete("protected/{resourceId:int}/claims/{claim}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<ActionResult> DeleteProtectedResourceClaim([FromRoute]int resourceId, [FromRoute]string claim) {
            var apiResource = await _configurationDbContext.ApiResources.Include(x => x.UserClaims).SingleOrDefaultAsync(x => x.Id == resourceId);
            if (apiResource == null) {
                return NotFound();
            }
            if (apiResource.UserClaims == null) {
                apiResource.UserClaims = new List<ApiResourceClaim>();
            }
            var claimToRemove = apiResource.UserClaims.Select(x => x.Type == claim).ToList();
            if (claimToRemove?.Count == 0) {
                return NotFound();
            }
            apiResource.UserClaims.RemoveAll(x => x.Type == claim);
            await _configurationDbContext.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Adds claims to an API resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <param name="request">Contains info about the API scope to be created.</param>
        /// <response code="200">Ok</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("protected/{resourceId:int}/scopes")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<ActionResult> AddProtectedResourceScope([FromRoute]int resourceId, [FromBody]CreateResourceRequest request) {
            var apiResource = await _configurationDbContext.ApiResources.SingleOrDefaultAsync(x => x.Id == resourceId);
            if (apiResource == null) {
                return NotFound();
            }
            if (apiResource.Scopes == null) {
                apiResource.Scopes = new List<ApiScope>();
            }
            apiResource.Scopes.Add(new ApiScope {
                Name = request.Name,
                DisplayName = request.DisplayName,
                Description = request.Description,
                UserClaims = request.UserClaims.Select(x => new ApiScopeClaim {
                    Type = x
                })
                .ToList()
            });
            await _configurationDbContext.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Adds claims to an API scope of a protected resource.
        /// </summary>
        /// <param name="resourceId">The identifier of the API resource.</param>
        /// <param name="scopeId">The identifier of the API resource scope.</param>
        /// <param name="claims">The claims to add to the scope.</param>
        /// <response code="200">Ok</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("protected/{resourceId:int}/scopes/{scopeId:int}/claims")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<ActionResult> AddProtectedResourceScopeClaims([FromRoute]int resourceId, [FromRoute]int scopeId, [FromBody]string[] claims) {
            var apiResourceScope = await _configurationDbContext.ApiResources
                                                                .Where(x => x.Id == resourceId)
                                                                .SelectMany(x => x.Scopes)
                                                                .Where(x => x.Id == scopeId)
                                                                .SingleOrDefaultAsync();
            if (apiResourceScope == null) {
                return NotFound();
            }
            if (apiResourceScope.UserClaims == null) {
                apiResourceScope.UserClaims = new List<ApiScopeClaim>();
            }
            apiResourceScope.UserClaims.AddRange(claims.Select(x => new ApiScopeClaim {
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
        /// <response code="200">Ok</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpDelete("protected/{resourceId:int}/scopes/{scopeId:int}/claims/{claim}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<ActionResult> DeleteProtectedResourceScopeClaim([FromRoute]int resourceId, [FromRoute]int scopeId, [FromRoute]string claim) {
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
    }
}
