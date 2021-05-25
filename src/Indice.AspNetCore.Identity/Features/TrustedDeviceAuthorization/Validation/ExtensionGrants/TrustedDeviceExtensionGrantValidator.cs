using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Configuration;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Models;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Services;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Stores;
using Indice.Configuration;
using Indice.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Validation
{
    internal class TrustedDeviceExtensionGrantValidator : RequestChallengeValidator, IExtensionGrantValidator
    {
        public TrustedDeviceExtensionGrantValidator(
            IAuthorizationCodeChallengeStore codeChallengeStore,
            IDevicePasswordHasher devicePasswordHasher,
            ISystemClock systemClock,
            IUserDeviceStore userDeviceStore
        ) {
            CodeChallengeStore = codeChallengeStore ?? throw new ArgumentNullException(nameof(codeChallengeStore));
            DevicePasswordHasher = devicePasswordHasher ?? throw new ArgumentNullException(nameof(devicePasswordHasher));
            SystemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
            UserDeviceStore = userDeviceStore ?? throw new ArgumentNullException(nameof(userDeviceStore));
        }

        public string GrantType => CustomGrantTypes.TrustedDevice;
        public IAuthorizationCodeChallengeStore CodeChallengeStore { get; }
        public IDevicePasswordHasher DevicePasswordHasher { get; }
        public ISystemClock SystemClock { get; }
        public IUserDeviceStore UserDeviceStore { get; }

        public async Task ValidateAsync(ExtensionGrantValidationContext context) {
            var parameters = context.Request.Raw;
            var deviceId = parameters.Get(RegistrationRequestParameters.DeviceId);
            if (string.IsNullOrWhiteSpace(deviceId)) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, $"Parameter '{RegistrationRequestParameters.DeviceId}' is not specified.");
                return;
            }
            // Load device.
            var device = await UserDeviceStore.GetByDeviceId(deviceId);
            if (device == null || !device.Enabled) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Device is unknown or not enabled.");
                return;
            }
            // If a code is present we are heading towards fingerprint login.
            var code = parameters.Get(RegistrationRequestParameters.Code);
            if (!string.IsNullOrWhiteSpace(code)) {
                // Retrieve authorization code from the store.
                var authorizationCode = await CodeChallengeStore.GetAuthorizationCode(code);
                if (authorizationCode == null) {
                    context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Authorization code is invalid.");
                    return;
                }
                // Validate that the consumer specified all required parameters.
                var parametersToValidate = new List<string> {
                    RegistrationRequestParameters.CodeSignature,
                    RegistrationRequestParameters.CodeVerifier
                };
                foreach (var parameter in parametersToValidate) {
                    var parameterValue = parameters.Get(parameter);
                    if (string.IsNullOrWhiteSpace(parameterValue)) {
                        context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, $"Parameter '{parameter}' is not specified.");
                        return;
                    }
                }
                // Validate authorization code against code verifier given by the client.
                var codeVerifier = parameters.Get(RegistrationRequestParameters.CodeVerifier);
                var authorizationCodeValidationResult = await ValidateAuthorizationCode(code, authorizationCode, codeVerifier, deviceId, context.Request.Client);
                if (authorizationCodeValidationResult.IsError) {
                    context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, authorizationCodeValidationResult.ErrorDescription);
                    return;
                }
                // Validate given public key against signature for fingerprint.
                var publicKey = parameters.Get(RegistrationRequestParameters.PublicKey);
                var codeSignature = parameters.Get(RegistrationRequestParameters.CodeSignature);
                var publicKeyValidationResult = ValidateSignature(publicKey, code, codeSignature);
                if (publicKeyValidationResult.IsError) {
                    context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, publicKeyValidationResult.ErrorDescription);
                    return;
                }
                await UserDeviceStore.UpdateDevicePublicKey(device, publicKey);
                // Grant access token.
                context.Result = new GrantValidationResult(authorizationCode.Subject.GetSubjectId(), GrantType);
            }
            var pin = parameters.Get(RegistrationRequestParameters.Pin);
            if (!string.IsNullOrWhiteSpace(pin)) {
                var result = DevicePasswordHasher.VerifyHashedPassword(device, device.Password, pin);
                if (result == PasswordVerificationResult.Failed) {
                    context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Wrong pin.");
                    return;
                }
                context.Result = new GrantValidationResult(device.UserId, GrantType);
            }
        }

        private async Task<ValidationResult> ValidateAuthorizationCode(string code, TrustedDeviceAuthorizationCode authorizationCode, string codeVerifier, string deviceId, Client client) {
            // Validate that the current client is not trying to use an authorization code of a different client.
            if (authorizationCode.ClientId != client.ClientId) {
                return Invalid("Authorization code is invalid.");
            }
            // Validate that the current device is not trying to use an authorization code of a different device.
            if (authorizationCode.DeviceId != deviceId) {
                return Invalid("Authorization code is invalid.");
            }
            // Remove authorization code.
            await CodeChallengeStore.RemoveAuthorizationCode(code);
            // Validate code expiration.
            if (authorizationCode.CreationTime.HasExceeded(authorizationCode.Lifetime, SystemClock.UtcNow.UtcDateTime)) {
                return Invalid("Authorization code is invalid.");
            }
            if (authorizationCode.CreationTime.HasExceeded(client.AuthorizationCodeLifetime, SystemClock.UtcNow.UtcDateTime)) {
                return Invalid("Authorization code is invalid.");
            }
            if (authorizationCode.RequestedScopes == null || !authorizationCode.RequestedScopes.Any()) {
                return Invalid("Authorization code is invalid.");
            }
            var proofKeyParametersValidationResult = ValidateAuthorizationCodeWithProofKeyParameters(codeVerifier, authorizationCode);
            if (proofKeyParametersValidationResult.IsError) {
                return Invalid(proofKeyParametersValidationResult.ErrorDescription);
            }
            return Success();
        }
    }
}
