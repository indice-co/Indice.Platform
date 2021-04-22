using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Configuration;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Validation
{
    internal class InitRegistrationRequestValidator : RegistrationRequestValidatorBase<InitRegistrationRequestValidationResult>
    {
        public InitRegistrationRequestValidator(
            IClientStore clientStore,
            ILogger<InitRegistrationRequestValidator> logger,
            ITokenValidator tokenValidator
        ) : base(clientStore, tokenValidator) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ILogger<InitRegistrationRequestValidator> Logger { get; set; }

        public override async Task<InitRegistrationRequestValidationResult> Validate(string accessToken, NameValueCollection parameters) {
            Logger.LogDebug($"{nameof(InitRegistrationRequestValidator)}: Started trusted device registration request validation.");
            // The access token needs to be valid and have at least the openid scope.
            var tokenValidationResult = await TokenValidator.ValidateAccessTokenAsync(accessToken, IdentityServerConstants.StandardScopes.OpenId);
            if (tokenValidationResult.IsError) {
                return Error(tokenValidationResult.Error, "Provided access token is not valid.");
            }
            // The access token must have a 'sub' and 'client_id' claim.
            var claimsToValidate = new[] { JwtClaimTypes.Subject, JwtClaimTypes.ClientId };
            foreach (var claim in claimsToValidate) {
                var claimValue = tokenValidationResult.Claims.SingleOrDefault(x => x.Type == claim)?.Value;
                if (string.IsNullOrWhiteSpace(claimValue)) {
                    return Error(OidcConstants.ProtectedResourceErrors.InvalidToken, $"Access token must contain the '{claim}' claim.");
                }
            }
            // Validate that the consumer specified the 'mode', 'device_id', 'code_challenge' and 'code_challenge_method' parameters.
            var parametersToValidate = new[] {
                RegistrationRequestParameters.CodeChallenge,
                RegistrationRequestParameters.CodeChallengeMethod,
                RegistrationRequestParameters.DeviceId,
                RegistrationRequestParameters.Mode
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
            // Load client and validate that it allows the 'password' flow.
            var client = await LoadClient(tokenValidationResult);
            if (client == null) {
                return Error(OidcConstants.AuthorizeErrors.UnauthorizedClient, $"Client is unknown or not enabled.");
            }
            if (!client.AllowedGrantTypes.Contains(GrantType.ResourceOwnerPassword)) {
                return Error(OidcConstants.AuthorizeErrors.UnauthorizedClient, $"Client not authorized for 'password' grant type.");
            }
            // Find requested scopes.
            var requestedScopes = tokenValidationResult.Claims.Where(claim => claim.Type == JwtClaimTypes.Scope).Select(claim => claim.Value).ToList();
            // Create principal from incoming access token excluding protocol claims.
            var claims = tokenValidationResult.Claims.Where(x => !ProtocolClaimsFilter.Contains(x.Type));
            var principal = Principal.Create("TrustedDevice", claims.ToArray());
            var userId = tokenValidationResult.Claims.Single(x => x.Type == JwtClaimTypes.Subject).Value;
            // Finally return result.
            return new InitRegistrationRequestValidationResult {
                Client = client,
                CodeChallenge = parameters.Get(RegistrationRequestParameters.CodeChallenge),
                CodeChallengeMethod = parameters.Get(RegistrationRequestParameters.CodeChallengeMethod),
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
