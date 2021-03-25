using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Identity.Features
{
    internal class TrustedDeviceRegistrationRequestValidator : ITrustedDeviceRegistrationRequestValidator
    {
        private readonly ILogger<TrustedDeviceRegistrationRequestValidator> _logger;
        private readonly ITokenValidator _tokenValidator;
        private readonly IClientStore _clientStore;
        private static readonly string[] _protocolClaimsFilter = {
            JwtClaimTypes.AccessTokenHash,
            JwtClaimTypes.Audience,
            JwtClaimTypes.AuthorizedParty,
            JwtClaimTypes.AuthorizationCodeHash,
            JwtClaimTypes.ClientId,
            JwtClaimTypes.Expiration,
            JwtClaimTypes.IssuedAt,
            JwtClaimTypes.Issuer,
            JwtClaimTypes.JwtId,
            JwtClaimTypes.Nonce,
            JwtClaimTypes.NotBefore,
            JwtClaimTypes.ReferenceTokenId,
            JwtClaimTypes.SessionId,
            JwtClaimTypes.Scope
        };

        public TrustedDeviceRegistrationRequestValidator(
            ILogger<TrustedDeviceRegistrationRequestValidator> logger,
            ITokenValidator tokenValidator,
            IClientStore clientStore
        ) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(logger));
            _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
        }

        public async Task<TrustedDeviceRegistrationRequestValidationResult> Validate(string accessToken, NameValueCollection parameters) {
            _logger.LogDebug("Started trusted device registration request validation.");
            // The access token needs to be valid and have at least the openid scope.
            var tokenResult = await _tokenValidator.ValidateAccessTokenAsync(accessToken, IdentityServerConstants.StandardScopes.OpenId);
            if (tokenResult.IsError) {
                return Error(tokenResult.Error);
            }
            // The access token must have a one 'sub' claim.
            var subjectClaim = tokenResult.Claims.SingleOrDefault(claim => claim.Type == JwtClaimTypes.Subject);
            if (subjectClaim == null) {
                _logger.LogError("Token does not contain a 'sub' claim.");
                return Error(OidcConstants.ProtectedResourceErrors.InvalidToken, "Token must contain the 'sub' claim.");
            }
            // The access token must have a one 'client_id' claim.
            var clientIdClaim = tokenResult.Claims.SingleOrDefault(claim => claim.Type == JwtClaimTypes.ClientId);
            if (clientIdClaim == null) {
                _logger.LogError("Token does not contain a 'client_id' claim.");
                return Error(OidcConstants.ProtectedResourceErrors.InvalidToken, "Token must contain the 'client_id' claim.");
            }
            // Load client.
            var client = await _clientStore.FindEnabledClientByIdAsync(clientIdClaim.Value);
            if (client == null) {
                _logger.LogError("Client with id '{0}' is unknown or not enabled.", clientIdClaim.Value);
                return Error(OidcConstants.AuthorizeErrors.UnauthorizedClient, "Unknown client or client not enabled.");
            }
            // Find requested scopes.
            var requestedScopes = tokenResult.Claims.Where(claim => claim.Type == JwtClaimTypes.Scope).Select(claim => claim.Value).ToList();
            // Create principal from incoming access token excluding protocol claims.
            var claims = tokenResult.Claims.Where(x => !_protocolClaimsFilter.Contains(x.Type));
            var principal = Principal.Create("TrustedDevice", claims.ToArray());
            return new TrustedDeviceRegistrationRequestValidationResult {
                IsError = false,
                Principal = principal,
                Client = client,
                RequestedScopes = requestedScopes
            };
        }

        private TrustedDeviceRegistrationRequestValidationResult Error(string error, string errorDescription = null) => new() {
            IsError = true,
            Error = error,
            ErrorDescription = errorDescription
        };
    }
}
