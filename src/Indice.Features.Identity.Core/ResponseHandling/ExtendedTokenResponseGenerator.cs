using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Indice.Features.Identity.Core.Grants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.Core.ResponseHandling;

/// <summary>Token response generator containing extensions.</summary>
public class ExtendedTokenResponseGenerator : TokenResponseGenerator
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Creates a new instance of <see cref="ExtendedTokenResponseGenerator"/>.</summary>
    /// <param name="clock">Abstracts the system clock to facilitate testing.</param>
    /// <param name="tokenService">Logic for creating security tokens.</param>
    /// <param name="refreshTokenService">Implements refresh token creation and validation.</param>
    /// <param name="scopeParser">Allows parsing raw scopes values into structured scope values.</param>
    /// <param name="resources">Resource retrieval.</param>
    /// <param name="clients">Retrieval of client configuration.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <param name="serviceProvider">Defines a mechanism for retrieving a service object.</param>
    public ExtendedTokenResponseGenerator(
        ISystemClock clock,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        IScopeParser scopeParser,
        IResourceStore resources,
        IClientStore clients,
        ILogger<TokenResponseGenerator> logger,
        IServiceProvider serviceProvider
    ) : base(clock, tokenService, refreshTokenService, scopeParser, resources, clients, logger) {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    protected override async Task<TokenResponse> ProcessPasswordRequestAsync(TokenRequestValidationResult request) {
        var tokenResponse = await base.ProcessPasswordRequestAsync(request);
        var config = _serviceProvider.GetService<ResourceOwnerPasswordValidatorOptions>();
        if (config?.IncludeIdToken == false) {
            return tokenResponse;
        }
        if (request.ValidatedRequest.RequestedScopes.Contains(IdentityServerConstants.StandardScopes.OpenId)) {
            Client client = null;
            if (!string.IsNullOrWhiteSpace(request.ValidatedRequest.ClientId)) {
                client = await Clients.FindEnabledClientByIdAsync(request.ValidatedRequest.ClientId);
            }
            if (client is null) {
                throw new InvalidOperationException("Client does not exist anymore.");
            }
            var parsedScopesResult = ScopeParser.ParseScopeValues(request.ValidatedRequest.RequestedScopes);
            var validatedResources = await Resources.CreateResourceValidationResult(parsedScopesResult);
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
}
