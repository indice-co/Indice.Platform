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
    internal class InitRegistrationRequestValidator : RegistrationRequestValidatorBase<InitRegistrationRequestValidationResult>
    {
        private readonly ILogger<InitRegistrationRequestValidator> _logger;

        public InitRegistrationRequestValidator(
            IClientStore clientStore,
            ILogger<InitRegistrationRequestValidator> logger,
            ITokenValidator tokenValidator
        ) : base(clientStore, tokenValidator) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<InitRegistrationRequestValidationResult> Validate(string accessToken, NameValueCollection parameters) {
            _logger.LogDebug($"{nameof(InitRegistrationRequestValidator)}: Started trusted device registration request validation.");
            // The access token needs to be valid and have at least the openid scope.
            var tokenResult = await TokenValidator.ValidateAccessTokenAsync(accessToken, IdentityServerConstants.StandardScopes.OpenId);
            if (tokenResult.IsError) {
                return Error(tokenResult.Error, "Provided access token is not valid.");
            }
            // The access token must have a 'sub' claim.
            var subjectClaim = tokenResult.Claims.SingleOrDefault(claim => claim.Type == JwtClaimTypes.Subject);
            if (subjectClaim == null) {
                return Error(OidcConstants.ProtectedResourceErrors.InvalidToken, $"Access token must contain the '{nameof(JwtClaimTypes.Subject)}' claim.");
            }
            // The access token must have a 'client_id' claim.
            var clientIdClaim = tokenResult.Claims.SingleOrDefault(claim => claim.Type == JwtClaimTypes.ClientId);
            if (clientIdClaim == null) {
                return Error(OidcConstants.ProtectedResourceErrors.InvalidToken, $"Access token must contain the '{nameof(JwtClaimTypes.ClientId)}' claim.");
            }
            // Check if the consumer specified the desired interaction.
            var modeString = parameters.Get(RegistrationRequestParameters.Mode);
            var mode = RegistrationRequestParameters.GetInteractionMode(modeString);
            if (!mode.HasValue) {
                return Error(OidcConstants.TokenErrors.InvalidRequest, $"Parameter '{nameof(RegistrationRequestParameters.Mode)}' used for registration (fingerprint or 4pin) is not specified.");
            }
            // Check if the consumer specified the device id.
            var deviceId = parameters.Get(RegistrationRequestParameters.DeviceId);
            if (string.IsNullOrWhiteSpace(deviceId)) {
                return Error(OidcConstants.TokenErrors.InvalidRequest, $"Parameter '{nameof(RegistrationRequestParameters.DeviceId)}' is not specified.");
            }
            // Check if the consumer specified a code challenge and the used hash function.
            var codeChallenge = parameters.Get(RegistrationRequestParameters.CodeChallenge);
            var codeChallengeMethod = parameters.Get(RegistrationRequestParameters.CodeChallengeMethod);
            if (string.IsNullOrWhiteSpace(codeChallenge) || string.IsNullOrWhiteSpace(codeChallengeMethod)) {
                return Error(OidcConstants.TokenErrors.InvalidRequest, $"Parameter '{nameof(RegistrationRequestParameters.CodeChallenge)}' or '{RegistrationRequestParameters.CodeChallengeMethod}' is not specified.");
            }
            // Load client.
            var client = await ClientStore.FindEnabledClientByIdAsync(clientIdClaim.Value);
            if (client == null) {
                return Error(OidcConstants.AuthorizeErrors.UnauthorizedClient, $"Client with id '{clientIdClaim.Value}' is unknown or not enabled.");
            }
            // Find requested scopes.
            var requestedScopes = tokenResult.Claims.Where(claim => claim.Type == JwtClaimTypes.Scope).Select(claim => claim.Value).ToList();
            // Create principal from incoming access token excluding protocol claims.
            var claims = tokenResult.Claims.Where(x => !ProtocolClaimsFilter.Contains(x.Type));
            var principal = Principal.Create("TrustedDevice", claims.ToArray());
            return new InitRegistrationRequestValidationResult {
                Client = client,
                CodeChallenge = codeChallenge,
                CodeChallengeMethod = codeChallengeMethod,
                DeviceId = deviceId,
                InteractionMode = mode.Value,
                IsError = false,
                Principal = principal,
                RequestedScopes = requestedScopes,
                UserId = subjectClaim.Value
            };
        }
    }
}
