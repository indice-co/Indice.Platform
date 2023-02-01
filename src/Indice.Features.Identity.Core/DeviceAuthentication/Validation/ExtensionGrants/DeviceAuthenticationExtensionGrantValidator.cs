using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Indice.Features.Identity.Core.DeviceAuthentication.Configuration;
using Indice.Features.Identity.Core.DeviceAuthentication.Models;
using Indice.Features.Identity.Core.DeviceAuthentication.Services;
using Indice.Features.Identity.Core.DeviceAuthentication.Stores;
using Indice.Configuration;
using Indice.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Indice.Features.Identity.Core.Models;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Validation
{
    internal class DeviceAuthenticationExtensionGrantValidator : RequestChallengeValidator, IExtensionGrantValidator
    {
        public DeviceAuthenticationExtensionGrantValidator(
            IDeviceAuthenticationCodeChallengeStore codeChallengeStore,
            IDevicePasswordHasher devicePasswordHasher,
            ISystemClock systemClock,
            IUserDeviceStore userDeviceStore,
            ExtendedUserManager<User> userManager
        ) {
            CodeChallengeStore = codeChallengeStore ?? throw new ArgumentNullException(nameof(codeChallengeStore));
            DevicePasswordHasher = devicePasswordHasher ?? throw new ArgumentNullException(nameof(devicePasswordHasher));
            SystemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
            UserDeviceStore = userDeviceStore ?? throw new ArgumentNullException(nameof(userDeviceStore));
            UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public string GrantType => CustomGrantTypes.DeviceAuthentication;
        public IDeviceAuthenticationCodeChallengeStore CodeChallengeStore { get; }
        public IDevicePasswordHasher DevicePasswordHasher { get; }
        public ISystemClock SystemClock { get; }
        public IUserDeviceStore UserDeviceStore { get; }
        public ExtendedUserManager<User> UserManager { get; }

        public async Task ValidateAsync(ExtensionGrantValidationContext context) {
            var parameters = context.Request.Raw;
            // Load device.
            var isValidRegistrationId = Guid.TryParse(parameters.Get(RegistrationRequestParameters.RegistrationId), out var registrationId);
            if (!isValidRegistrationId) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidTarget, "Device registration id is not valid.");
                return;
            }
            var device = await UserDeviceStore.GetById(registrationId);
            if (device is null) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidTarget, "Device is unknown.");
                return;
            }
            if (device.RequiresPassword) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidTarget, ExtraTokenRequestErrors.RequiresPassword);
                return;
            }
            // Load user.
            var user = await UserManager.FindByIdAsync(device.UserId);
            if (user is null) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidTarget, "User does not exists.");
                return;
            }
            var code = parameters.Get(RegistrationRequestParameters.Code);
            var pin = parameters.Get(RegistrationRequestParameters.Pin);
            var hasCode = !string.IsNullOrWhiteSpace(code);
            var hasPin = !string.IsNullOrWhiteSpace(pin);
            var loginStrategyValues = new bool[] { hasCode, hasPin };
            if (loginStrategyValues.All(x => x == true) || loginStrategyValues.All(x => x == false)) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Please provider either authorization code of pin.");
                return;
            }
            // If code is present we are heading towards fingerprint login.
            if (hasCode) {
                // Retrieve authorization code from the store.
                var authorizationCode = await CodeChallengeStore.GetDeviceAuthenticationCode(code);
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
                var authorizationCodeValidationResult = await ValidateAuthorizationCode(code, authorizationCode, codeVerifier, registrationId, context.Request.Client);
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
                await UserDeviceStore.UpdatePublicKey(device, publicKey);
                // Grant access token.
                context.Result = new GrantValidationResult(authorizationCode.Subject.GetSubjectId(), GrantType);
            }
            // If pin is present we are heading towards a 4-Pin login.
            if (hasPin) {
                var result = DevicePasswordHasher.VerifyHashedPassword(device, device.Password, pin);
                if (result == PasswordVerificationResult.Failed) {
                    context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Wrong pin.");
                    return;
                }
                context.Result = new GrantValidationResult(device.UserId, GrantType);
            }
            await UserDeviceStore.UpdateLastSignInDate(device);
            await UserManager.SetLastSignInDateAsync(user, DateTimeOffset.UtcNow);
        }

        private async Task<ValidationResult> ValidateAuthorizationCode(string code, DeviceAuthenticationCode authorizationCode, string codeVerifier, Guid registrationId, Client client) {
            // Validate that the current client is not trying to use an authorization code of a different client.
            if (authorizationCode.ClientId != client.ClientId) {
                return Invalid("Authorization code is invalid.");
            }
            // Validate that the current device is not trying to use an authorization code of a different device.
            if (Guid.Parse(authorizationCode.DeviceId) != registrationId) {
                return Invalid("Authorization code is invalid.");
            }
            // Remove authorization code.
            await CodeChallengeStore.RemoveDeviceAuthenticationCode(code);
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
