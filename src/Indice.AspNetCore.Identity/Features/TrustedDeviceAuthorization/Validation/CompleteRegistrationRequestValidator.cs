using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Identity.Features
{
    internal class CompleteRegistrationRequestValidator : RegistrationRequestValidatorBase<CompleteRegistrationRequestValidationResult>
    {
        private readonly ILogger<CompleteRegistrationRequestValidator> _logger;

        public CompleteRegistrationRequestValidator(
            IClientStore clientStore,
            ILogger<CompleteRegistrationRequestValidator> logger,
            ITokenValidator tokenValidator
        ) : base(clientStore, tokenValidator) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<CompleteRegistrationRequestValidationResult> Validate(string accessToken, NameValueCollection parameters) {
            _logger.LogDebug($"[{nameof(CompleteRegistrationRequestValidator)}] Started trusted device registration request validation.");
            // The access token needs to be valid and have at least the openid scope.
            var tokenResult = await TokenValidator.ValidateAccessTokenAsync(accessToken, IdentityServerConstants.StandardScopes.OpenId);
            if (tokenResult.IsError) {
                return Error(tokenResult.Error, "Provided access token is not valid.");
            }
            // The access token must have a 'sub' and 'client_id' claim.
            var claimsToValidate = new[] { JwtClaimTypes.Subject, JwtClaimTypes.ClientId };
            foreach (var claim in claimsToValidate) {
                var claimValue = tokenResult.Claims.SingleOrDefault(x => x.Type == claim)?.Value;
                if (string.IsNullOrWhiteSpace(claimValue)) {
                    return Error(OidcConstants.ProtectedResourceErrors.InvalidToken, $"Access token must contain the '{claim}' claim.");
                }
            }
            // The consumer must specify the 'mode', 'device_id', 'code_signature', 'code_verifier', 'public_key', 'code' and 'otp' parameters.
            var parametersToValidate = new[] { 
                RegistrationRequestParameters.Mode, 
                RegistrationRequestParameters.DeviceId, 
                RegistrationRequestParameters.CodeSignature, 
                RegistrationRequestParameters.CodeVerifier,
                RegistrationRequestParameters.PublicKey,
                RegistrationRequestParameters.OtpCode,
                RegistrationRequestParameters.Code
            };
            foreach (var parameter in parametersToValidate) {
                var parameterValue = parameters.Get(parameter);
                if (string.IsNullOrWhiteSpace(parameterValue)) {
                    return Error(OidcConstants.TokenErrors.InvalidRequest, $"Parameter '{parameter}' is not specified.");
                }
            }
            var mode = RegistrationRequestParameters.GetInteractionMode(parameters.Get(RegistrationRequestParameters.Mode));
            if (!mode.HasValue) {
                return Error(OidcConstants.TokenErrors.InvalidRequest, $"Parameter '{nameof(RegistrationRequestParameters.Mode)}' used for registration (fingerprint or 4pin) is not valid.");
            }
            // Load client.
            var clientId = tokenResult.Claims.Single(x => x.Type == JwtClaimTypes.ClientId).Value;
            var client = await ClientStore.FindEnabledClientByIdAsync(clientId);
            if (client == null) {
                return Error(OidcConstants.AuthorizeErrors.UnauthorizedClient, $"Client with id '{clientId}' is unknown or not enabled.");
            }
            // Find requested scopes.
            var requestedScopes = tokenResult.Claims.Where(claim => claim.Type == JwtClaimTypes.Scope).Select(claim => claim.Value).ToList();
            // Create principal from incoming access token excluding protocol claims.
            var claims = tokenResult.Claims.Where(x => !ProtocolClaimsFilter.Contains(x.Type));
            var principal = Principal.Create("TrustedDevice", claims.ToArray());
            var userId = tokenResult.Claims.Single(x => x.Type == JwtClaimTypes.Subject).Value;
            return new CompleteRegistrationRequestValidationResult {
                Client = client,
                DeviceId = parameters.Get(RegistrationRequestParameters.DeviceId),
                InteractionMode = mode.Value,
                IsError = false,
                Principal = principal,
                RequestedScopes = requestedScopes,
                UserId = userId
            };
        }
    }
}
