using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity.Api.Configuration;
using Indice.AspNetCore.Identity.Api.Events;
using Indice.AspNetCore.Identity.Api.Filters;
using Indice.AspNetCore.Identity.Api.Models;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.Configuration;
using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Core.Extensions;
using Indice.Features.Identity.Core.Models;
using Indice.Security;
using Indice.Serialization;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Client = IdentityServer4.EntityFramework.Entities.Client;
using ClientClaim = IdentityServer4.EntityFramework.Entities.ClientClaim;

namespace Indice.AspNetCore.Identity.Api.Controllers;

/// <summary>Contains operations for managing application clients.</summary>
/// <response code="400">Bad Request</response>
/// <response code="401">Unauthorized</response>
/// <response code="403">Forbidden</response>
[ApiController]
[ApiExplorerSettings(GroupName = "identity")]
[ProblemDetailsExceptionFilter]
[ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
[Route("api/clients")]
internal class ClientsController : ControllerBase
{
    private readonly ExtendedConfigurationDbContext _configurationDbContext;
    private readonly GeneralSettings _generalSettings;
    private readonly IPlatformEventService _eventService;
    private readonly IdentityServerApiEndpointsOptions _apiEndpointsOptions;
    /// <summary>The name of the controller.</summary>
    public const string Name = "Clients";

    /// <summary>Creates an instance of <see cref="ClientsController"/>.</summary>
    /// <param name="configurationDbContext">Abstraction for the configuration context.</param>
    /// <param name="generalSettings">Applications general settings.</param>
    /// <param name="eventService">Models the event mechanism used to raise events inside the IdentityServer API.</param>
    /// <param name="apiEndpointsOptions">Options for configuring the IdentityServer API feature.</param>
    public ClientsController(
        ExtendedConfigurationDbContext configurationDbContext,
        IOptionsSnapshot<GeneralSettings> generalSettings,
        IPlatformEventService eventService,
        IdentityServerApiEndpointsOptions apiEndpointsOptions
    ) {
        _configurationDbContext = configurationDbContext ?? throw new ArgumentNullException(nameof(configurationDbContext));
        _generalSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _apiEndpointsOptions = apiEndpointsOptions ?? throw new ArgumentNullException(nameof(apiEndpointsOptions));
    }

    public string UserId => User.FindFirstValue(JwtClaimTypes.Subject);

    /// <summary>Returns a list of <see cref="ClientInfo"/> objects containing the total number of clients in the database and the data filtered according to the provided <see cref="ListOptions"/>.</summary>
    /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
    /// <response code="200">OK</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeClientsReader)]
    [Consumes(MediaTypeNames.Application.Json)]
    [HttpGet]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<ClientInfo>))]
    public async Task<IActionResult> GetClients([FromQuery] ListOptions options) {
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
            RequireConsent = x.RequireConsent,
            NonEditable = x.NonEditable
        })
        .ToResultSetAsync(options);
        return Ok(clients);
    }

    /// <summary>Gets a client by it's unique id.</summary>
    /// <param name="clientId">The identifier of the client.</param>
    /// <response code="200">OK</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeClientsReader)]
    [CacheResourceFilter]
    [Consumes(MediaTypeNames.Application.Json)]
    [HttpGet("{clientId}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SingleClientInfo))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> GetClient([FromRoute] string clientId) {
        var client = await _configurationDbContext
            .Clients
            .Include(x => x.Properties)
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
                AllowedCorsOrigins = x.AllowedCorsOrigins.Select(x => x.Origin),
                PostLogoutRedirectUris = x.PostLogoutRedirectUris.Select(x => x.PostLogoutRedirectUri),
                RedirectUris = x.RedirectUris.Select(x => x.RedirectUri),
                IdentityTokenLifetime = x.IdentityTokenLifetime,
                AccessTokenLifetime = x.AccessTokenLifetime,
                ConsentLifetime = x.ConsentLifetime,
                UserSsoLifetime = x.UserSsoLifetime,
                FrontChannelLogoutUri = x.FrontChannelLogoutUri,
                PairWiseSubjectSalt = x.PairWiseSubjectSalt,
                AccessTokenType = (AccessTokenType)x.AccessTokenType,
                FrontChannelLogoutSessionRequired = x.FrontChannelLogoutSessionRequired,
                IncludeJwtId = x.IncludeJwtId,
                AllowAccessTokensViaBrowser = x.AllowAccessTokensViaBrowser,
                AlwaysIncludeUserClaimsInIdToken = x.AlwaysIncludeUserClaimsInIdToken,
                AlwaysSendClientClaims = x.AlwaysSendClientClaims,
                AuthorizationCodeLifetime = x.AuthorizationCodeLifetime,
                RequirePkce = x.RequirePkce,
                AllowPlainTextPkce = x.AllowPlainTextPkce,
                ClientClaimsPrefix = x.ClientClaimsPrefix,
                GrantTypes = x.AllowedGrantTypes.Select(x => x.GrantType),
                AbsoluteRefreshTokenLifetime = x.AbsoluteRefreshTokenLifetime,
                AllowOfflineAccess = x.AllowOfflineAccess,
                NonEditable = x.NonEditable,
                RefreshTokenExpiration = (TokenExpiration)x.RefreshTokenExpiration,
                RefreshTokenUsage = (TokenUsage)x.RefreshTokenUsage,
                UpdateAccessTokenClaimsOnRefresh = x.UpdateAccessTokenClaimsOnRefresh,
                BackChannelLogoutUri = x.BackChannelLogoutUri,
                BackChannelLogoutSessionRequired = x.BackChannelLogoutSessionRequired,
                UserCodeType = x.UserCodeType,
                DeviceCodeLifetime = x.DeviceCodeLifetime,
                SlidingRefreshTokenLifetime = x.SlidingRefreshTokenLifetime,
                EnableLocalLogin = x.EnableLocalLogin,
                IdentityProviderRestrictions = x.IdentityProviderRestrictions.Select(x => x.Provider),
                ApiResources = x.AllowedScopes.Join(
                    _configurationDbContext.ApiResources.SelectMany(x => x.Scopes),
                    clientScope => clientScope.Scope,
                    apiScope => apiScope.Scope,
                    (clientScope, apiScope) => apiScope.Scope
                )
                .Select(x => x),
                IdentityResources = x.AllowedScopes.Join(
                    _configurationDbContext.IdentityResources,
                    clientScope => clientScope.Scope,
                    identityResource => identityResource.Name,
                    (clientScope, identityResource) => identityResource.Name
                )
                .Select(x => x),
                Claims = x.Claims.Select(x => new ClaimInfo {
                    Id = x.Id,
                    Type = x.Type,
                    Value = x.Value
                }),
                Secrets = x.ClientSecrets.Select(x => new ClientSecretInfo {
                    Id = x.Id,
                    Type = x.Type,
                    Value = GetClientSecretValue(x),
                    Description = x.Description,
                    Expiration = x.Expiration
                }),
                Translations = TranslationDictionary<ClientTranslation>.FromJson(x.Properties.Any(clientProperty => clientProperty.Key == ClientPropertyKeys.Translation)
                    ? x.Properties.Single(clientProperty => clientProperty.Key == ClientPropertyKeys.Translation).Value
                    : string.Empty)
            })
            .SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return NotFound();
        }
        return Ok(client);
    }

    /// <summary>Creates a new client.</summary>
    /// <param name="request">Contains info about the client to be created.</param>
    /// <response code="201">Created</response>
    /// <response code="400">Bad Request</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeClientsWriter)]
    [CacheResourceFilter(DependentStaticPaths = new string[] { "api/dashboard/summary" })]
    [Consumes(MediaTypeNames.Application.Json)]
    [HttpPost]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(ClientInfo))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientRequest request) {
        var clientIdExists = (await _configurationDbContext.Clients.CountAsync(x => x.ClientId == request.ClientId)) > 0;
        if (clientIdExists) {
            ModelState.AddModelError(nameof(request.ClientId), $"Client with id '{request.ClientId}' already exists.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        var client = CreateForType(request.ClientType, _generalSettings.Authority, request);
        _configurationDbContext.Clients.Add(client);
        _configurationDbContext.ClientUsers.Add(new ClientUser {
            Client = client,
            UserId = UserId
        });
        await _configurationDbContext.SaveChangesAsync();
        await _eventService.Publish(new ClientCreatedEvent(client.ToModel()));
        var response = ClientInfo.FromClient(client);
        return CreatedAtAction(nameof(GetClient), new { clientId = client.ClientId }, response);
    }

    /// <summary>Updates an existing client.</summary>
    /// <param name="clientId">The id of the client.</param>
    /// <param name="request">Contains info about the client to be updated.</param>
    /// <response code="204">No Content</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeClientsWriter)]
    [CacheResourceFilter]
    [Consumes(MediaTypeNames.Application.Json)]
    [HttpPut("{clientId}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> UpdateClient([FromRoute] string clientId, [FromBody] UpdateClientRequest request) {
        var client = await _configurationDbContext.Clients.Include(x => x.Properties).Include(x => x.IdentityProviderRestrictions).SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return NotFound();
        }
        client.AbsoluteRefreshTokenLifetime = request.AbsoluteRefreshTokenLifetime;
        client.AccessTokenLifetime = request.AccessTokenLifetime;
        client.AccessTokenType = (int)request.AccessTokenType;
        client.AllowAccessTokensViaBrowser = request.AllowAccessTokensViaBrowser;
        client.AllowOfflineAccess = request.AllowOfflineAccess;
        client.AllowPlainTextPkce = request.AllowPlainTextPkce;
        client.AllowRememberConsent = request.AllowRememberConsent;
        client.AlwaysIncludeUserClaimsInIdToken = request.AlwaysIncludeUserClaimsInIdToken;
        client.AlwaysSendClientClaims = request.AlwaysSendClientClaims;
        client.AuthorizationCodeLifetime = request.AuthorizationCodeLifetime;
        client.BackChannelLogoutSessionRequired = request.BackChannelLogoutSessionRequired;
        client.BackChannelLogoutUri = request.BackChannelLogoutUri;
        client.ClientClaimsPrefix = request.ClientClaimsPrefix;
        client.ClientName = request.ClientName;
        client.ClientUri = request.ClientUri;
        client.ConsentLifetime = request.ConsentLifetime;
        client.Description = request.Description;
        client.DeviceCodeLifetime = request.DeviceCodeLifetime;
        client.Enabled = request.Enabled;
        client.FrontChannelLogoutSessionRequired = request.FrontChannelLogoutSessionRequired;
        client.FrontChannelLogoutUri = request.FrontChannelLogoutUri;
        client.IdentityTokenLifetime = request.IdentityTokenLifetime;
        client.IncludeJwtId = request.IncludeJwtId;
        client.LogoUri = request.LogoUri;
        client.PairWiseSubjectSalt = request.PairWiseSubjectSalt;
        client.RefreshTokenExpiration = (int)request.RefreshTokenExpiration;
        client.RefreshTokenUsage = (int)request.RefreshTokenUsage;
        client.RequireConsent = request.RequireConsent;
        client.RequirePkce = request.RequirePkce;
        client.UpdateAccessTokenClaimsOnRefresh = request.UpdateAccessTokenClaimsOnRefresh;
        client.UserCodeType = request.UserCodeType;
        client.UserSsoLifetime = request.UserSsoLifetime;
        client.SlidingRefreshTokenLifetime = request.SlidingRefreshTokenLifetime;
        if (request.EnableLocalLogin.HasValue) {
            client.EnableLocalLogin = request.EnableLocalLogin.Value;
        }
        client.IdentityProviderRestrictions.RemoveAll(x => true);
        client.IdentityProviderRestrictions.AddRange(request.IdentityProviderRestrictions.Select(provider => new ClientIdPRestriction {
            Provider = provider,
            Client = client
        }));
        var clientTranslations = client.Properties?.SingleOrDefault(x => x.Key == ClientPropertyKeys.Translation);
        if (clientTranslations is null) {
            AddClientTranslations(client, request.Translations.ToJson());
        } else {
            clientTranslations.Value = request.Translations.ToJson() ?? string.Empty;
        }
        await _configurationDbContext.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Adds a claim for the specified client.</summary>
    /// <param name="clientId">The id of the client.</param>
    /// <param name="request">The claim to add.</param>
    /// <response code="201">Created</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeClientsWriter)]
    [CacheResourceFilter(DependentPaths = new string[] { "{clientId}" })]
    [Consumes(MediaTypeNames.Application.Json)]
    [HttpPost("{clientId}/claims")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(ClaimInfo))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> AddClientClaim([FromRoute] string clientId, [FromBody] CreateClaimRequest request) {
        var client = await _configurationDbContext.Clients.SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return NotFound();
        }
        var claimToAdd = new ClientClaim {
            Client = client,
            ClientId = client.Id,
            Type = request.Type,
            Value = request.Value
        };
        client.Claims = new List<ClientClaim> {
            claimToAdd
        };
        await _configurationDbContext.SaveChangesAsync();
        return Created(string.Empty, new ClaimInfo {
            Id = claimToAdd.Id,
            Type = claimToAdd.Type,
            Value = claimToAdd.Value
        });
    }

    /// <summary>Removes an identity resource from the specified client.</summary>
    /// <param name="clientId">The id of the client.</param>
    /// <param name="claimId">The id of the claim to delete.</param>
    /// <response code="204">No Content</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeClientsWriter)]
    [CacheResourceFilter(DependentPaths = new string[] { "{clientId}" })]
    [Consumes(MediaTypeNames.Application.Json)]
    [HttpDelete("{clientId}/claims/{claimId:int}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent)]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> DeleteClientClaim([FromRoute] string clientId, [FromRoute] int claimId) {
        var client = await _configurationDbContext.Clients.Include(x => x.Claims).SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return NotFound();
        }
        if (client.Claims == null) {
            client.Claims = new List<ClientClaim>();
        }
        var claimToRemove = client.Claims.SingleOrDefault(x => x.Id == claimId);
        if (claimToRemove == null) {
            return NotFound();
        }
        client.Claims.Remove(claimToRemove);
        await _configurationDbContext.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Renews the list of </summary>
    /// <param name="clientId">The id of the client.</param>
    /// <param name="request"></param>
    /// <response code="204">No Content</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeClientsWriter)]
    [CacheResourceFilter(DependentPaths = new string[] { "{clientId}" })]
    [Consumes(MediaTypeNames.Application.Json)]
    [HttpPost("{clientId}/urls")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> UpdateClientUrls([FromRoute] string clientId, [FromBody] UpdateClientUrls request) {
        var client = await _configurationDbContext
            .Clients
            .Include(x => x.AllowedCorsOrigins)
            .Include(x => x.RedirectUris)
            .Include(x => x.PostLogoutRedirectUris)
            .SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return NotFound();
        }
        client.AllowedCorsOrigins?.RemoveAll(x => true);
        client.RedirectUris?.RemoveAll(x => true);
        client.PostLogoutRedirectUris?.RemoveAll(x => true);
        if (request.AllowedCorsOrigins?.Count() > 0) {
            client.AllowedCorsOrigins.AddRange(request.AllowedCorsOrigins.Select(x => new ClientCorsOrigin {
                ClientId = client.Id,
                Origin = x.TrimEnd('/')
            }));
        }
        if (request.RedirectUris?.Count() > 0) {
            client.RedirectUris.AddRange(request.RedirectUris.Select(x => new ClientRedirectUri {
                ClientId = client.Id,
                RedirectUri = x
            }));
        }
        if (request.PostLogoutRedirectUris?.Count() > 0) {
            client.PostLogoutRedirectUris.AddRange(request.PostLogoutRedirectUris.Select(x => new ClientPostLogoutRedirectUri {
                ClientId = client.Id,
                PostLogoutRedirectUri = x
            }));
        }
        await _configurationDbContext.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Adds an identity resource to the specified client.</summary>
    /// <param name="clientId">The id of the client.</param>
    /// <param name="resources">The API or identity resources to add.</param>
    /// <response code="204">No Content</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeClientsWriter)]
    [CacheResourceFilter(DependentPaths = new string[] { "{clientId}" })]
    [Consumes(MediaTypeNames.Application.Json)]
    [HttpPost("{clientId}/resources")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> AddClientResources([FromRoute] string clientId, [FromBody] string[] resources) {
        var client = await _configurationDbContext.Clients.SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return NotFound();
        }
        client.AllowedScopes = new List<ClientScope>();
        client.AllowedScopes.AddRange(resources.Select(x => new ClientScope {
            ClientId = client.Id,
            Scope = x
        }));
        await _configurationDbContext.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Removes an identity resource from the specified client.</summary>
    /// <param name="clientId">The id of the client.</param>
    /// <param name="resources">The names of the identity resources to delete.</param>
    /// <response code="204">No Content</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeClientsWriter)]
    [CacheResourceFilter(DependentPaths = new string[] { "{clientId}" })]
    [Consumes(MediaTypeNames.Application.Json)]
    [HttpDelete("{clientId}/resources")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> DeleteClientResource([FromRoute] string clientId, [FromBody] string[] resources) {
        var client = await _configurationDbContext.Clients.Include(x => x.AllowedScopes).SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return NotFound();
        }
        if (client.AllowedScopes == null) {
            client.AllowedScopes = new List<ClientScope>();
        }
        var resourcesToRemove = client.AllowedScopes.Where(x => resources.Contains(x.Scope)).ToList();
        if (resourcesToRemove == null) {
            return NotFound();
        }
        foreach (var resource in resourcesToRemove) {
            client.AllowedScopes.Remove(resource);
        }
        await _configurationDbContext.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Adds an identity resource to the specified client.</summary>
    /// <param name="clientId">The id of the client.</param>
    /// <param name="grantType">The name of the grant type to add.</param>
    /// <response code="200">OK</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeClientsWriter)]
    [CacheResourceFilter(DependentPaths = new string[] { "{clientId}" })]
    [Consumes(MediaTypeNames.Application.Json)]
    [HttpPost("{clientId}/grant-types/{grantType}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(GrantTypeInfo))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> AddClientGrantType([FromRoute] string clientId, [FromRoute] string grantType) {
        var client = await _configurationDbContext.Clients.SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return NotFound();
        }
        var grantTypeToAdd = new ClientGrantType {
            GrantType = grantType,
            ClientId = client.Id
        };
        client.AllowedGrantTypes = new List<ClientGrantType> {
            grantTypeToAdd
        };
        await _configurationDbContext.SaveChangesAsync();
        return Ok(new GrantTypeInfo {
            Id = grantTypeToAdd.Id,
            Name = grantTypeToAdd.GrantType
        });
    }

    /// <summary>Removes an identity resource from the specified client.</summary>
    /// <param name="clientId">The id of the client.</param>
    /// <param name="grantType">The id of the resource to delete.</param>
    /// <response code="204">No Content</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeClientsWriter)]
    [CacheResourceFilter(DependentPaths = new string[] { "{clientId}" })]
    [Consumes(MediaTypeNames.Application.Json)]
    [HttpDelete("{clientId}/grant-types/{grantType}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent)]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> DeleteClientGrantType([FromRoute] string clientId, [FromRoute] string grantType) {
        var client = await _configurationDbContext.Clients.Include(x => x.AllowedGrantTypes).SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return NotFound();
        }
        if (client.AllowedGrantTypes == null) {
            client.AllowedGrantTypes = new List<ClientGrantType>();
        }
        var grantTypeToRemove = client.AllowedGrantTypes.SingleOrDefault(x => x.GrantType == grantType);
        if (grantTypeToRemove == null) {
            return NotFound();
        }
        client.AllowedGrantTypes.Remove(grantTypeToRemove);
        await _configurationDbContext.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Adds a new secret to an existing client.</summary>
    /// <param name="clientId">The id of the client.</param>
    /// <param name="request">Contains info about the API scope to be created.</param>
    /// <response code="201">Created</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeClientsWriter)]
    [CacheResourceFilter(DependentPaths = new string[] { "{clientId}" })]
    [Consumes(MediaTypeNames.Application.Json)]
    [HttpPost("{clientId}/secrets")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(SecretInfo))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> AddClientSecret([FromRoute] string clientId, [FromBody] CreateSecretRequest request) {
        var client = await _configurationDbContext.Clients.SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client is null) {
            return NotFound();
        }
        var newSecret = new ClientSecret {
            Description = request.Description,
            Value = request.Value.ToSha256(),
            Expiration = request.Expiration,
            Type = IdentityServerConstants.SecretTypes.SharedSecret,
            ClientId = client.Id
        };
        client.ClientSecrets = new List<ClientSecret> { newSecret };
        await _configurationDbContext.SaveChangesAsync();
        return CreatedAtAction(string.Empty, new SecretInfo {
            Id = newSecret.Id,
            Description = newSecret.Description,
            Expiration = newSecret.Expiration,
            Type = newSecret.Type,
            Value = GetClientSecretValue(newSecret)
        });
    }

    /// <summary>Adds a new secret, from a certificate, to an existing client.</summary>
    /// <param name="clientId">The id of the client.</param>
    /// <param name="file">The file to be uploaded.</param>
    /// <response code="201">Created</response>
    /// <response code="400">Bad Request</response>
    /// <response code="404">Not Found</response>
    [AllowedFileExtensions(".cer")]
    [AllowedFileSize(2097152)] // 2 MegaBytes
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeClientsWriter)]
    [CacheResourceFilter(DependentPaths = new string[] { "{clientId}" })]
    [Consumes("multipart/form-data")]
    [HttpPost("{clientId}/certificates")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(SecretInfo))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> UploadCertificate([FromRoute] string clientId, [FromForm] IFormFile file) {
        if (file == null) {
            ModelState.AddModelError(string.Empty, "Please upload a certificate.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        var memoryStream = new MemoryStream();
        X509Certificate2 certificate;
        try {
            await file.CopyToAsync(memoryStream);
            var certificateBytes = memoryStream.ToArray();
            certificate = new X509Certificate2(certificateBytes);
        } catch (CryptographicException) {
            ModelState.AddModelError(string.Empty, "Uploaded certificate is not valid.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        } finally {
            await memoryStream.DisposeAsync();
        }
        var client = await _configurationDbContext.Clients.SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return NotFound();
        }
        var newSecret = new ClientSecret {
            Description = certificate.Subject,
            Value = Convert.ToBase64String(certificate.GetRawCertData()),
            Expiration = DateTime.Parse(certificate.GetExpirationDateString()),
            Type = IdentityServerConstants.SecretTypes.X509CertificateBase64,
            ClientId = client.Id
        };
        client.ClientSecrets = new List<ClientSecret> { newSecret };
        await _configurationDbContext.SaveChangesAsync();
        return CreatedAtAction(string.Empty, new SecretInfo {
            Id = newSecret.Id,
            Description = newSecret.Description,
            Expiration = newSecret.Expiration,
            Type = newSecret.Type,
            Value = GetClientSecretValue(newSecret)
        });
    }

    /// <summary>Gets the metadata of a certificate for display.</summary>
    /// <param name="file">The file to be uploaded.</param>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="404">Not Found</response>
    [AllowedFileExtensions(".cer")]
    [AllowedFileSize(2097152)] // 2 MegaBytes
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeClientsReader)]
    [Consumes("multipart/form-data")]
    [HttpPost("certificates")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SecretInfoBase))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> GetCertificateMetadata([FromForm] IFormFile file) {
        if (file == null) {
            ModelState.AddModelError(string.Empty, "Please upload a certificate.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        var memoryStream = new MemoryStream();
        X509Certificate2 certificate;
        try {
            await file.CopyToAsync(memoryStream);
            var certificateBytes = memoryStream.ToArray();
            certificate = new X509Certificate2(certificateBytes);
        } catch (CryptographicException) {
            ModelState.AddModelError(string.Empty, "Uploaded certificate is not valid.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        } finally {
            await memoryStream.DisposeAsync();
        }
        var secretInfo = new SecretInfoBase {
            Description = certificate.Subject,
            Value = $"Thumbprint: {certificate.Thumbprint}, Expiration Date: {certificate.GetExpirationDateString()}",
            Expiration = DateTime.Parse(certificate.GetExpirationDateString()),
            Type = IdentityServerConstants.SecretTypes.X509CertificateBase64
        };
        return Ok(secretInfo);
    }

    /// <summary>Downloads a client secret if it is a certificate.</summary>
    /// <param name="clientId">The id of the client.</param>
    /// <param name="clientSecretId">The id of the client secret.</param>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeClientsReader)]
    [Consumes(MediaTypeNames.Application.Json)]
    [HttpGet("{clientId}/certificates/{clientSecretId:int}")]
    [Produces(MediaTypeNames.Application.Json, "application/x-x509-user-cert")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IFormFile))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> GetCertificate([FromRoute] string clientId, [FromRoute] int clientSecretId) {
        var client = await _configurationDbContext.Clients.Include(x => x.ClientSecrets).SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return NotFound();
        }
        var clientSecret = client.ClientSecrets.FirstOrDefault(clientSecret => clientSecret.Id == clientSecretId);
        if (clientSecret == null) {
            return NotFound();
        }
        if (clientSecret.Type != IdentityServerConstants.SecretTypes.X509CertificateBase64) {
            ModelState.AddModelError(string.Empty, $"A secret of type {clientSecret.Type} cannot be downloaded.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        X509Certificate2 certificate = null;
        byte[] certificateBytes;
        try {
            certificate = new X509Certificate2(Convert.FromBase64String(clientSecret.Value));
            certificateBytes = certificate.Export(X509ContentType.Cert);
        } catch (CryptographicException) {
            throw;
        } finally {
            certificate?.Dispose();
        }
        return File(certificateBytes, "application/x-x509-user-cert", $"{client.ClientId}.cer");
    }

    /// <summary>Removes a specified secret from a client.</summary>
    /// <param name="clientId">The id of the client.</param>
    /// <param name="secretId">The identifier of the client secret to remove.</param>
    /// <response code="204">No Content</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeClientsWriter)]
    [CacheResourceFilter(DependentPaths = new string[] { "{clientId}" })]
    [Consumes(MediaTypeNames.Application.Json)]
    [HttpDelete("{clientId}/secrets/{secretId}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> DeleteClientSecret([FromRoute] string clientId, [FromRoute] int secretId) {
        var client = await _configurationDbContext.Clients.Include(x => x.ClientSecrets).SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return NotFound();
        }
        if (client.ClientSecrets == null) {
            client.ClientSecrets = new List<ClientSecret>();
        }
        var secretToRemove = client.ClientSecrets.SingleOrDefault(x => x.Id == secretId);
        if (secretToRemove == null) {
            return NotFound();
        }
        client.ClientSecrets.Remove(secretToRemove);
        await _configurationDbContext.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Permanently deletes an existing client.</summary>
    /// <param name="clientId">The id of the client to delete.</param>
    /// <response code="204">No Content</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeClientsWriter)]
    [CacheResourceFilter(DependentStaticPaths = new string[] { "api/dashboard/summary" })]
    [Consumes(MediaTypeNames.Application.Json)]
    [HttpDelete("{clientId}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> DeleteClient([FromRoute] string clientId) {
        var client = await _configurationDbContext.Clients.AsNoTracking().SingleOrDefaultAsync(x => x.ClientId == clientId);
        if (client == null) {
            return NotFound();
        }
        _configurationDbContext.Clients.Remove(client);
        await _configurationDbContext.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Gets the UI configuration for the specified client.</summary>
    /// <param name="clientId">The id of the client.</param>
    /// <response code="400">Bad Request</response>
    /// <response code="404">No Content</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeClientsWriter)]
    [Consumes(MediaTypeNames.Application.Json)]
    [HttpGet("{clientId}/theme")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ClientThemeConfigResponse))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> GetClientTheme([FromRoute] string clientId) {
        var client = await _configurationDbContext
            .Clients
            .Include(x => x.Properties)
            .Where(x => x.ClientId == clientId)
            .SingleOrDefaultAsync();
        if (client is null) {
            ModelState.AddModelError(nameof(Client.Id), "Requested client does not exist.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        var themeConfigResolver = HttpContext.RequestServices.GetRequiredService<ClientThemeConfigTypeResolver>();
        var themeConfig = client.Properties.Where(x => x.Key == ClientPropertyKeys.ThemeConfig).FirstOrDefault();
        return Ok(new ClientThemeConfigResponse {
            Schema = themeConfigResolver.Resolve().ToJsonSchema().AsJsonElement(),
            Data = themeConfig is not null ? JsonSerializer.Deserialize<DefaultClientThemeConfig>(themeConfig.Value, JsonSerializerOptionDefaults.GetDefaultSettings()) : null
        });
    }

    /// <summary>Creates or updates the ui configuration for the specified client.</summary>
    /// <param name="clientId">The id of the client.</param>
    /// <param name="request">The request data describing the configuration.</param>
    /// <response code="204">No Content</response>
    /// <response code="400">Bad Request</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeClientsWriter)]
    [Consumes(MediaTypeNames.Application.Json)]
    [HttpPut("{clientId}/theme")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    public async Task<IActionResult> CreateOrUpdateClientTheme([FromRoute] string clientId, [FromBody] ClientThemeConfigRequest request) {
        var client = await _configurationDbContext
            .Clients
            .Include(x => x.Properties)
            .Where(x => x.ClientId == clientId)
            .SingleOrDefaultAsync();
        if (client is null) {
            ModelState.AddModelError(nameof(Client.Id), "Requested client does not exist.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        var themeConfig = client.Properties.Where(x => x.Key == ClientPropertyKeys.ThemeConfig).FirstOrDefault();
        var themeConfigValue = JsonSerializer.Serialize(request, JsonSerializerOptionDefaults.GetDefaultSettings());
        if (themeConfig is null) {
            client.Properties.Add(new ClientProperty {
                Client = client,
                ClientId = client.Id,
                Key = ClientPropertyKeys.ThemeConfig,
                Value = themeConfigValue
            });
        } else {
            themeConfig.Value = themeConfigValue;
        }
        await _configurationDbContext.SaveChangesAsync();
        return NoContent();
    }

    private static string GetClientSecretValue(ClientSecret clientSecret) {
        switch (clientSecret.Type) {
            case IdentityServerConstants.SecretTypes.SharedSecret:
            case IdentityServerConstants.SecretTypes.JsonWebKey:
                return $"{clientSecret.Value.Substring(0, 3)}***********";
            case IdentityServerConstants.SecretTypes.X509CertificateBase64:
                var certificateData = Convert.FromBase64String(clientSecret.Value);
                using (var certificate = new X509Certificate2(certificateData)) {
                    return $"Thumbprint: {certificate.Thumbprint}, Expiration Date: {certificate.GetExpirationDateString()}";
                }
            case IdentityServerConstants.SecretTypes.X509CertificateName:
            case IdentityServerConstants.SecretTypes.X509CertificateThumbprint:
                return clientSecret.Value;
            default:
                throw new ArgumentOutOfRangeException(nameof(clientSecret.Type));
        }
    }

    /// <summary>Creates default client configuration based on <see cref="ClientType"/>.</summary>
    /// <param name="clientType">The type of the client.</param>
    /// <param name="authorityUri">The IdentityServer instance URI.</param>
    /// <param name="clientRequest">Client information provided by the user.</param>
    private static Client CreateForType(ClientType clientType, string authorityUri, CreateClientRequest clientRequest) {
        var client = new Client {
            ClientId = clientRequest.ClientId,
            ClientName = clientRequest.ClientName,
            Description = clientRequest.Description,
            ClientUri = clientRequest.ClientUri,
            LogoUri = clientRequest.LogoUri,
            RequireConsent = clientRequest.RequireConsent,
            BackChannelLogoutSessionRequired = true,
            AllowedScopes = clientRequest.IdentityResources.Union(clientRequest.ApiResources).Select(scope => new ClientScope { Scope = scope }).ToList(),
            EnableLocalLogin = true,
            Enabled = true
        };
        if (!string.IsNullOrEmpty(clientRequest.RedirectUri)) {
            client.RedirectUris = new List<ClientRedirectUri> {
                new ClientRedirectUri { RedirectUri = clientRequest.RedirectUri }
            };
        }
        if (!string.IsNullOrEmpty(clientRequest.PostLogoutRedirectUri)) {
            client.PostLogoutRedirectUris = new List<ClientPostLogoutRedirectUri> {
                new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = clientRequest.PostLogoutRedirectUri }
            };
        }
        if (clientRequest.Secrets.Any()) {
            client.ClientSecrets = clientRequest.Secrets.Select(x => new ClientSecret {
                Type = IdentityServerConstants.SecretTypes.SharedSecret,
                Description = x.Description,
                Expiration = x.Expiration,
                Value = x.Value.ToSha256()
            })
            .ToList();
        }
        if (clientRequest.Translations.Any()) {
            AddClientTranslations(client, clientRequest.Translations.ToJson());
        }
        switch (clientType) {
            case ClientType.SPA:
                client.AllowedGrantTypes = new List<ClientGrantType> {
                    new ClientGrantType {
                        GrantType = GrantType.AuthorizationCode
                    }
                };
                client.RequirePkce = true;
                client.RequireClientSecret = false;
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
                client.RequirePkce = true;
                break;
            case ClientType.Native:
                client.AllowedGrantTypes = new List<ClientGrantType> {
                    new ClientGrantType {
                        GrantType = GrantType.AuthorizationCode
                    }
                };
                client.RequirePkce = true;
                client.RequireClientSecret = false;
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
                client.AllowedGrantTypes = new List<ClientGrantType> {
                    new ClientGrantType {
                        GrantType = GrantType.DeviceFlow
                    }
                };
                break;
            case ClientType.SPALegacy:
                client.AllowedGrantTypes = new List<ClientGrantType> {
                    new ClientGrantType {
                        GrantType = GrantType.Implicit
                    }
                };
                client.RequirePkce = false;
                client.RequireClientSecret = false;
                client.AllowAccessTokensViaBrowser = true;
                client.AllowedCorsOrigins = new List<ClientCorsOrigin> {
                    new ClientCorsOrigin {
                        Origin = clientRequest.ClientUri ?? authorityUri
                    }
                };
                break;
            default:
                throw new ArgumentNullException(nameof(clientType), "Cannot determine the type of the client.");
        }
        return client;
    }

    /// <summary>Adds translations to a <see cref="Client"/>.</summary>
    /// <remarks>If the parameter translations is null, string.Empty will be persisted.</remarks>
    /// <param name="client">The <see cref="Client"/></param>
    /// <param name="translations">The JSON string with the translations</param>
    private static void AddClientTranslations(Client client, string translations) {
        client.Properties ??= new List<ClientProperty>();
        client.Properties.Add(new ClientProperty {
            Key = ClientPropertyKeys.Translation,
            Value = translations ?? string.Empty,
            Client = client
        });
    }
}
