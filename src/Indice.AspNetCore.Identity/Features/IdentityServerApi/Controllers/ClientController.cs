using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.Models;
using Indice.AspNetCore.Extensions;
using Indice.Configuration;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Entities = IdentityServer4.EntityFramework.Entities;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Contains operations for managing application clients.
    /// </summary>
    [GenericControllerNameConvention]
    [Route("api/clients")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.SubScopes.Clients)]
    internal class ClientController : ControllerBase
    {
        private readonly ExtendedConfigurationDbContext _configurationDbContext;
        private readonly GeneralSettings _generalSettings;
        private readonly IDistributedCache _cache;
        private readonly IEventService _eventService;
        private readonly IdentityServerApiEndpointsOptions _apiEndpointsOptions;
        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "Client";

        /// <summary>
        /// Creates an instance of <see cref="ClientController"/>.
        /// </summary>
        /// <param name="configurationDbContext">Abstraction for the configuration context.</param>
        /// <param name="generalSettings">Applications general settings.</param>
        /// <param name="cache">Represents a distributed cache of serialized values.</param>
        /// <param name="eventService">Models the event mechanism used to raise events inside the IdentityServer API.</param>
        /// <param name="apiEndpointsOptions">Options for configuring the IdentityServer API feature.</param>
        public ClientController(ExtendedConfigurationDbContext configurationDbContext, IOptions<GeneralSettings> generalSettings, IDistributedCache cache, IEventService eventService, IdentityServerApiEndpointsOptions apiEndpointsOptions) {
            _configurationDbContext = configurationDbContext ?? throw new ArgumentNullException(nameof(configurationDbContext));
            _generalSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _apiEndpointsOptions = apiEndpointsOptions ?? throw new ArgumentNullException(nameof(apiEndpointsOptions));
        }

        public string UserId => User.FindFirstValue(JwtClaimTypes.Subject);

        /// <summary>
        /// Returns a list of <see cref="ClientInfo"/> objects containing the total number of clients in the database and the data filtered according to the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <param name="options">List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<ClientInfo>))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<ActionResult<ResultSet<ClientInfo>>> GetClients([FromQuery]ListOptions options) {
            IQueryable<Entities.Client> query = null;
            if (User.IsAdmin()) {
                query = _configurationDbContext.Clients.AsQueryable();
            }
            if (!User.IsAdmin() && !string.IsNullOrEmpty(UserId)) {
                query = _configurationDbContext.ClientUsers.Include(x => x.Client).Where(x => x.UserId == UserId).Select(x => x.Client);
            }
            // If user is not an admin and user subject is not present in claims, then there is nothing to send.
            if (query == null) {
                return Ok(new ResultSet<ClientInfo>());
            }
            if (!string.IsNullOrEmpty(options.Search)) {
                var searchTerm = options.Search.ToLower();
                query = query.Where(x => x.ClientId.ToLower().Contains(searchTerm) || x.ClientName.Contains(searchTerm));
            }
            var clients = await query.Select(x => new ClientInfo {
                ClientId = x.ClientId,
                ClientName = x.ClientName,
                ClientUri = x.ClientUri,
                LogoUri = x.LogoUri,
                Description = x.Description,
                AllowRememberConsent = x.AllowRememberConsent,
                Enabled = x.Enabled,
                RequireConsent = x.RequireConsent
            })
            .ToResultSetAsync(options);
            return Ok(clients);
        }

        /// <summary>
        /// Gets a client by it's unique id.
        /// </summary>
        /// <param name="id">The identifier of the client.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SingleClientInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [HttpGet("{id}")]
        public async Task<ActionResult<SingleClientInfo>> GetClient([FromRoute]string id) {
            async Task<SingleClientInfo> GetClientAsync() {
                // Load client from the database.
                var query = _configurationDbContext.Clients.AsNoTracking();
                var foundClient = await query.Select(x => new SingleClientInfo {
                    ClientId = x.ClientId,
                    ClientName = x.ClientName,
                    ClientUri = x.ClientUri,
                    LogoUri = x.LogoUri,
                    Description = x.Description,
                    AllowRememberConsent = x.AllowRememberConsent,
                    Enabled = x.Enabled,
                    RequireConsent = x.RequireConsent,
                    AllowedCorsOrigins = x.AllowedCorsOrigins.Select(x => x.Origin).ToArray(),
                    PostLogoutRedirectUris = x.PostLogoutRedirectUris.Select(x => x.PostLogoutRedirectUri).ToArray(),
                    RedirectUris = x.RedirectUris.Select(x => x.RedirectUri).ToArray(),
                    IdentityTokenLifetime = x.IdentityTokenLifetime,
                    AccessTokenLifetime = x.AccessTokenLifetime,
                    ConsentLifetime = x.ConsentLifetime,
                    UserSsoLifetime = x.UserSsoLifetime,
                    FrontChannelLogoutUri = x.FrontChannelLogoutUri,
                    PairWiseSubjectSalt = x.PairWiseSubjectSalt,
                    AccessTokenType = x.AccessTokenType == 0 ? AccessTokenType.Jwt : AccessTokenType.Reference,
                    FrontChannelLogoutSessionRequired = x.FrontChannelLogoutSessionRequired,
                    IncludeJwtId = x.IncludeJwtId,
                    AllowAccessTokensViaBrowser = x.AllowAccessTokensViaBrowser,
                    AlwaysIncludeUserClaimsInIdToken = x.AlwaysIncludeUserClaimsInIdToken,
                    AlwaysSendClientClaims = x.AlwaysSendClientClaims,
                    AuthorizationCodeLifetime = x.AuthorizationCodeLifetime,
                    RequirePkce = x.RequirePkce,
                    AllowPlainTextPkce = x.AllowPlainTextPkce,
                    ClientClaimsPrefix = x.ClientClaimsPrefix,
                    GrantTypes = x.AllowedGrantTypes.Select(x => x.GrantType).ToArray(),
                    ApiResources = x.AllowedScopes.Join(
                        _configurationDbContext.ApiResources.SelectMany(x => x.Scopes),
                        clientScope => clientScope.Scope,
                        apiScope => apiScope.Name,
                        (clientScope, apiScope) => apiScope.Name
                    )
                    .Select(x => x)
                    .ToArray(),
                    IdentityResources = x.AllowedScopes.Join(
                        _configurationDbContext.IdentityResources,
                        clientScope => clientScope.Scope,
                        identityResource => identityResource.Name,
                        (clientScope, identityResource) => identityResource.Name
                    )
                    .Select(x => x)
                    .ToArray(),
                    Claims = x.Claims.Select(x => new ClaimInfo {
                        Id = x.Id,
                        Type = x.Type,
                        Value = x.Value
                    }),
                    Secrets = x.ClientSecrets.Select(x => new ClientSecretInfo {
                        Id = x.Id,
                        Type = x.Type == nameof(SecretType.SharedSecret) ? SecretType.SharedSecret : SecretType.X509Thumbprint,
                        Value = "*****",
                        Description = x.Description,
                        Expiration = x.Expiration
                    })
                })
                .SingleOrDefaultAsync(x => x.ClientId == id);
                if (foundClient == null) {
                    return null;
                }
                return foundClient;
            }
            // Retrieve the client by either the cache or the database.
            var client = await _cache.TryGetOrSetAsync(CacheKeys.Client(id), GetClientAsync, TimeSpan.FromDays(7));
            if (client == null) {
                return NotFound();
            }
            // Return 200 status code containing the client information.
            return Ok(client);
        }

        /// <summary>
        /// Creates a new client.
        /// </summary>
        /// <param name="request">Contains info about the client to be created.</param>
        /// <response code="201">Created</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(ClientInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<ActionResult<ClientInfo>> CreateClient([FromBody]CreateClientRequest request) {
            var client = CreateForType(request.ClientType, _generalSettings.Authority, request);
            _configurationDbContext.Clients.Add(client);
            await _configurationDbContext.SaveChangesAsync();
            var response = new ClientInfo {
                ClientId = client.ClientId,
                ClientName = client.ClientName,
                ClientUri = client.ClientUri,
                Description = client.Description,
                AllowRememberConsent = client.AllowRememberConsent,
                Enabled = client.Enabled,
                LogoUri = client.LogoUri,
                RequireConsent = client.RequireConsent
            };
            if (_apiEndpointsOptions.RaiseEvents) {
                await _eventService.Raise(new ClientCreatedEvent(response));
            }
            return CreatedAtAction(nameof(GetClient), new { id = client.ClientId }, response);
        }

        /// <summary>
        /// Adds a claim for the specified client.
        /// </summary>
        /// <param name="id">The id of the client.</param>
        /// <param name="request">The claim to add.</param>
        /// <response code="201">Created</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("{id}/claims")]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(ClaimInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<ActionResult<ClaimInfo>> AddClientClaim([FromRoute]string id, [FromBody]CreateClaimRequest request) {
            var client = await _configurationDbContext.Clients.SingleOrDefaultAsync(x => x.ClientId == id);
            if (client == null) {
                return NotFound();
            }
            var claimToAdd = new ClientClaim {
                Client = client,
                ClientId = client.Id,
                Type = request.Type,
                Value = request.Value
            };
            if (client.Claims == null) {
                client.Claims = new List<ClientClaim>();
            }
            client.Claims.Add(claimToAdd);
            await _configurationDbContext.SaveChangesAsync();
            await _cache.RemoveAsync(CacheKeys.Client(id));
            return Created(string.Empty, new ClaimInfo {
                Id = claimToAdd.Id,
                Type = claimToAdd.Type,
                Value = claimToAdd.Value
            });
        }

        /// <summary>
        /// Adds an identity resource to the specified client.
        /// </summary>
        /// <param name="id">The id of the client.</param>
        /// <param name="resources">The API or identity resources to add.</param>
        /// <response code="201">Created</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("{id}/resources")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<ActionResult> AddClientResources([FromRoute]string id, [FromBody]string[] resources) {
            var client = await _configurationDbContext.Clients.SingleOrDefaultAsync(x => x.ClientId == id);
            if (client == null) {
                return NotFound();
            }
            if (client.AllowedScopes == null) {
                client.AllowedScopes = new List<ClientScope>();
            }
            client.AllowedScopes.AddRange(resources.Select(x => new ClientScope {
                ClientId = client.Id,
                Scope = x
            }));
            await _configurationDbContext.SaveChangesAsync();
            await _cache.RemoveAsync(CacheKeys.Client(id));
            return Ok();
        }

        /// <summary>
        /// Permanently deletes an existing client.
        /// </summary>
        /// <param name="id">The id of the client to delete.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteClient([FromRoute]string id) {
            var client = await _configurationDbContext.Clients.AsNoTracking().SingleOrDefaultAsync(x => x.ClientId == id);
            if (client == null) {
                return NotFound();
            }
            _configurationDbContext.Clients.Remove(client);
            await _configurationDbContext.SaveChangesAsync();
            await _cache.RemoveAsync(CacheKeys.Client(id));
            return Ok();
        }

        /// <summary>
        /// Creates default client configuration based on <see cref="ClientType"/>.
        /// </summary>
        /// <param name="clientType">The type of the client.</param>
        /// <param name="authorityUri">The IdentityServer instance URI.</param>
        /// <param name="clientRequest">Client information provided by the user.</param>
        private Entities.Client CreateForType(ClientType clientType, string authorityUri, CreateClientRequest clientRequest) {
            var client = new Entities.Client {
                ClientId = clientRequest.ClientId,
                ClientName = clientRequest.ClientName,
                Description = clientRequest.Description,
                ClientUri = clientRequest.ClientUri,
                LogoUri = clientRequest.LogoUri,
                RequireConsent = clientRequest.RequireConsent,
                AllowedScopes = clientRequest.IdentityResources.Union(clientRequest.ApiResources).Select(scope => new ClientScope {
                    Scope = scope
                })
                .ToList()
            };
            if (!string.IsNullOrEmpty(clientRequest.RedirectUri)) {
                client.RedirectUris = new List<ClientRedirectUri> {
                    new ClientRedirectUri {
                        RedirectUri = clientRequest.RedirectUri
                    }
                };
            }
            if (!string.IsNullOrEmpty(clientRequest.PostLogoutRedirectUri)) {
                client.PostLogoutRedirectUris = new List<ClientPostLogoutRedirectUri> {
                    new ClientPostLogoutRedirectUri {
                        PostLogoutRedirectUri = clientRequest.PostLogoutRedirectUri
                    }
                };
            }
            if (clientRequest.Secrets.Any()) {
                client.ClientSecrets = clientRequest.Secrets.Select(x => new ClientSecret {
                    Type = $"{x.Type}",
                    Description = x.Description,
                    Expiration = x.Expiration,
                    Value = x.Value
                }).ToList();
            }
            switch (clientType) {
                case ClientType.SPA:
                    client.AllowedGrantTypes = new List<ClientGrantType> {
                        new ClientGrantType {
                            GrantType = GrantType.AuthorizationCode
                        }
                    };
                    client.RequirePkce = true;
                    client.AllowedCorsOrigins = new List<ClientCorsOrigin> {
                        new ClientCorsOrigin {
                            Origin = clientRequest.ClientUri ?? authorityUri
                        }
                    };
                    break;
                case ClientType.WebApp:
                    client.AllowedGrantTypes = new List<ClientGrantType> {
                        new ClientGrantType {
                            GrantType = GrantType.Hybrid
                        }
                    };
                    break;
                case ClientType.Native:
                    break;
                case ClientType.Machine:
                    client.AllowedGrantTypes = new List<ClientGrantType> {
                        new ClientGrantType {
                            GrantType = GrantType.ClientCredentials
                        }
                    };
                    client.RequireConsent = false;
                    break;
                case ClientType.Device:
                    break;
                case ClientType.SPALegacy:
                    client.AllowedGrantTypes = new List<ClientGrantType> {
                        new ClientGrantType {
                            GrantType = GrantType.Implicit
                        }
                    };
                    break;
                default:
                    throw new ArgumentNullException(nameof(clientType), "Cannot determine the type of the client.");
            }
            return client;
        }
    }
}
