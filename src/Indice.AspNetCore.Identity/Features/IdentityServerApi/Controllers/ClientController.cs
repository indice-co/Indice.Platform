using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Interfaces;
using IdentityServer4.Models;
using Indice.AspNetCore.Extensions;
using Indice.Configuration;
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
        private readonly IConfigurationDbContext _configurationDbContext;
        private readonly GeneralSettings _generalSettings;
        private readonly IDistributedCache _cache;
        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "Client";

        /// <summary>
        /// Creates an instance of <see cref="ClientController"/>.
        /// </summary>
        /// <param name="configurationDbContext">Abstraction for the configuration context.</param>
        /// <param name="generalSettings">Applications general settings.</param>
        public ClientController(IConfigurationDbContext configurationDbContext, IOptions<GeneralSettings> generalSettings, IDistributedCache cache) {
            _configurationDbContext = configurationDbContext ?? throw new ArgumentNullException(nameof(configurationDbContext));
            _generalSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

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
            var query = _configurationDbContext.Clients.AsQueryable();
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
                var foundClient = await _configurationDbContext.Clients
                                                               .Include(x => x.AllowedCorsOrigins)
                                                               .Include(x => x.PostLogoutRedirectUris)
                                                               .Include(x => x.RedirectUris)
                                                               .AsNoTracking()
                                                               .Select(x => new SingleClientInfo {
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
                                                                   RedirectUris = x.RedirectUris.Select(x => x.RedirectUri).ToArray()
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
            return CreatedAtAction(nameof(GetClient), new { id = client.ClientId }, new ClientInfo {
                ClientId = client.ClientId,
                ClientName = client.ClientName,
                ClientUri = client.ClientUri,
                Description = client.Description,
                AllowRememberConsent = client.AllowRememberConsent,
                Enabled = client.Enabled,
                LogoUri = client.LogoUri,
                RequireConsent = client.RequireConsent
            });
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
