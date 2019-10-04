using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.Interfaces;
using IdentityServer4.Stores;
using Indice.Configuration;
using Indice.Identity.Models;
using Indice.Identity.Security;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.Identity.Controllers.Api
{
    /// <summary>
    /// Contains operations for managing application clients.
    /// </summary>
    [Route("api/clients")]
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [Authorize(Policy = IdentityServerApi.SubScopes.Users)]
    public sealed class ClientController : ControllerBase
    {
        private readonly IClientStore _clientStore;
        private readonly IConfigurationDbContext _configurationDbContext;
        private readonly GeneralSettings _generalSettings;
        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "Client";

        /// <summary>
        /// Creates an instance of <see cref="ClientController"/>.
        /// </summary>
        /// <param name="clientStore">Retrieval of client configuration.</param>
        /// <param name="configurationDbContext">Abstraction for the configuration context.</param>
        /// <param name="generalSettings">Applications general settings.</param>
        public ClientController(IClientStore clientStore, IConfigurationDbContext configurationDbContext, IOptions<GeneralSettings> generalSettings) {
            _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
            _configurationDbContext = configurationDbContext ?? throw new ArgumentNullException(nameof(configurationDbContext));
            _generalSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
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
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ClientInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [HttpGet("{id}")]
        public async Task<ActionResult<ClientInfo>> GetClient([FromRoute]string id) {
            var client = await _configurationDbContext.Clients
                                                      .AsNoTracking()
                                                      .Select(x => new ClientInfo {
                                                          ClientId = x.ClientId,
                                                          ClientName = x.ClientName,
                                                          ClientUri = x.ClientUri,
                                                          LogoUri = x.LogoUri,
                                                          Description = x.Description,
                                                          AllowRememberConsent = x.AllowRememberConsent,
                                                          Enabled = x.Enabled,
                                                          RequireConsent = x.RequireConsent
                                                      })
                                                      .SingleOrDefaultAsync(x => x.ClientId == id);
            if (client == null) {
                return NotFound();
            }
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
            var client = Clients.CreateForType(request.ClientType, _generalSettings.Authority, request);
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
    }
}
