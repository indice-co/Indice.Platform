using System.Net;
#if NET9_0_OR_GREATER
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;
#else
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication;
#endif
using Indice.AspNetCore.Extensions;
using Indice.Features.Identity.Core.Grants;
using Indice.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.Core.ResponseHandling;


/// <summary>
/// Extends the functionality of the <see cref="TokenResponseGenerator"/> to include additional processing for token
/// responses.
/// </summary>
/// <remarks>This class provides enhanced capabilities for generating token responses, including support for
/// custom claims and additional identity token creation.</remarks>
public class ExtendedTokenResponseGenerator : TokenResponseGenerator
{
    /// <summary>Token response generator containing extensions.</summary>
    /// <remarks>Creates a new instance of <see cref="ExtendedTokenResponseGenerator"/>.</remarks>
    /// <param name="clock">Abstracts the system clock to facilitate testing.</param>
    /// <param name="tokenService">Logic for creating security tokens.</param>
    /// <param name="refreshTokenService">Implements refresh token creation and validation.</param>
    /// <param name="scopeParser">Allows parsing raw scopes values into structured scope values.</param>
    /// <param name="resources">Resource retrieval.</param>
    /// <param name="clients">Retrieval of client configuration.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <param name="serviceProvider">Defines a mechanism for retrieving a service object.</param>
    public ExtendedTokenResponseGenerator(

#if NET9_0_OR_GREATER
        IClock clock,
#else
        ISystemClock clock,
#endif
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        IScopeParser scopeParser,
        IResourceStore resources,
        IClientStore clients,
        ILogger<TokenResponseGenerator> logger,
        IServiceProvider serviceProvider) : base(
            clock,
            tokenService,
            refreshTokenService,
            scopeParser,
            resources,
            clients,
            logger) {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// The service provider scoped.
    /// </summary>
    protected IServiceProvider ServiceProvider { get; }

    /// <inheritdoc />
    protected override Task<TokenResponse> ProcessAuthorizationCodeRequestAsync(TokenRequestValidationResult request) {
        var httpContext = ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext!;
        var ip = httpContext.GetClientIpAddress();
        request.ValidatedRequest.Subject!.AddIdentity(new(claims: [
            new(BasicClaimTypes.IPAddress, ip == IPAddress.None ? string.Empty : ip.ToString())
        ]));
        return base.ProcessAuthorizationCodeRequestAsync(request);
    }

    /// <inheritdoc />
    protected override async Task<TokenResponse> ProcessPasswordRequestAsync(TokenRequestValidationResult request) {
        var tokenResponse = await base.ProcessPasswordRequestAsync(request);
        var config = ServiceProvider.GetService<ResourceOwnerPasswordValidatorOptions>();
        if (config?.IncludeIdToken == false) {
            return tokenResponse;
        }
        if (request.ValidatedRequest.RequestedScopes!.Contains(IdentityServerConstants.StandardScopes.OpenId)) {
            Client? client = null;
            if (!string.IsNullOrWhiteSpace(request.ValidatedRequest.ClientId)) {
                client = await Clients.FindEnabledClientByIdAsync(request.ValidatedRequest.ClientId);
            }
            if (client is null) {
                throw new InvalidOperationException("Client does not exist anymore.");
            }
            var parsedScopesResult = ScopeParser.ParseScopeValues(request.ValidatedRequest.RequestedScopes!);
            //var validatedResources = await Resources.CreateResourceValidationResult(parsedScopesResult);
            var validatedResources = await CreateResourceValidationResult(Resources, parsedScopesResult);
            //new ResourceValidationResult(parsedScopesResult, client, request.ValidatedRequest.RequestedScopes);
            var tokenRequest = new TokenCreationRequest {
                Subject = request.ValidatedRequest.Subject,
                ValidatedResources = validatedResources,
                AccessTokenToHash = tokenResponse.AccessToken,
                ValidatedRequest = request.ValidatedRequest
            };
            var idToken = await TokenService.CreateIdentityTokenAsync(tokenRequest);
            var jwt = await TokenService.CreateSecurityTokenAsync(idToken);
            tokenResponse.IdentityToken = jwt;
        }
        return tokenResponse;
    }

    /// <inheritdoc/>
    protected override async Task<TokenResponse> ProcessExtensionGrantRequestAsync(TokenRequestValidationResult request) {
        var httpContext = ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext!;
        var ip = httpContext.GetClientIpAddress();
        request.ValidatedRequest.Subject!.AddIdentity(new(claims: [
            new(BasicClaimTypes.IPAddress, ip == IPAddress.None ? string.Empty : ip.ToString())
        ]));
        var tokenResponse = await base.ProcessExtensionGrantRequestAsync(request);


        var config = ServiceProvider.GetService<ResourceOwnerPasswordValidatorOptions>();
        if (config?.IncludeIdToken == false) {
            return tokenResponse;
        }
        if (request.ValidatedRequest.RequestedScopes!.Contains(IdentityServerConstants.StandardScopes.OpenId)) {
            Client? client = null;
            if (!string.IsNullOrWhiteSpace(request.ValidatedRequest.ClientId)) {
                client = await Clients.FindEnabledClientByIdAsync(request.ValidatedRequest.ClientId);
            }
            if (client is null) {
                throw new InvalidOperationException("Client does not exist anymore.");
            }
            var parsedScopesResult = ScopeParser.ParseScopeValues(request.ValidatedRequest.RequestedScopes!);
            var validatedResources = await CreateResourceValidationResult(Resources, parsedScopesResult);
            var tokenRequest = new TokenCreationRequest {
                Subject = request.ValidatedRequest.Subject,
                ValidatedResources = validatedResources,
                AccessTokenToHash = tokenResponse.AccessToken,
                ValidatedRequest = request.ValidatedRequest
            };
            var idToken = await TokenService.CreateIdentityTokenAsync(tokenRequest);
            var jwt = await TokenService.CreateSecurityTokenAsync(idToken);
            tokenResponse.IdentityToken = jwt;
        }
        return tokenResponse;
    }


    /// <summary>
    /// Creates a resource validation result.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="parsedScopesResult">The parsed scopes.</param>
    /// <returns></returns>
    protected static async Task<ResourceValidationResult> CreateResourceValidationResult(IResourceStore store, ParsedScopesResult parsedScopesResult) {
        var validScopeValues = parsedScopesResult.ParsedScopes;
        var scopes = validScopeValues.Select(x => x.ParsedName).ToArray();
        var resources = await store.FindEnabledResourcesByScopeAsync(scopes);
        return new ResourceValidationResult(resources, validScopeValues);
    }
}
