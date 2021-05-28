using System;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Configuration;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Stores;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Validation
{
    internal class DeviceAuthorizationRequestValidator : RequestValidatorBase<DeviceAuthorizationRequestValidationResult>
    {
        public DeviceAuthorizationRequestValidator(
            IClientStore clientStore,
            ILogger<DeviceAuthorizationRequestValidator> logger,
            IResourceValidator resourceValidator,
            ITokenValidator tokenValidator,
            IUserDeviceStore userDeviceStore
        ) : base(clientStore, tokenValidator) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            UserDeviceStore = userDeviceStore ?? throw new ArgumentNullException(nameof(userDeviceStore));
            ResourceValidator = resourceValidator;
        }

        public ILogger<DeviceAuthorizationRequestValidator> Logger { get; }
        public IResourceValidator ResourceValidator { get; }
        public IUserDeviceStore UserDeviceStore { get; }

        public async override Task<DeviceAuthorizationRequestValidationResult> Validate(NameValueCollection parameters, string accessToken = null) {
            Logger.LogDebug($"{nameof(DeviceAuthorizationRequestValidator)}: Started trusted device authorization request validation.");
            // Validate that the consumer specified all required parameters.
            var parametersToValidate = new[] {
                RegistrationRequestParameters.ClientId,
                RegistrationRequestParameters.CodeChallenge,
                RegistrationRequestParameters.DeviceId,
                RegistrationRequestParameters.Scope
            };
            foreach (var parameter in parametersToValidate) {
                var parameterValue = parameters.Get(parameter);
                if (string.IsNullOrWhiteSpace(parameterValue)) {
                    return Error(OidcConstants.TokenErrors.InvalidRequest, $"Parameter '{parameter}' is not specified.");
                }
            }
            // Load device.
            var device = await UserDeviceStore.GetByDeviceId(parameters.Get(RegistrationRequestParameters.DeviceId));
            if (device == null || !device.SupportsFingerprintLogin) {
                return Error(OidcConstants.TokenErrors.InvalidRequest, "Device cannot initiate fingerprint login.");
            }
            // Load and validate client.
            var client = await LoadClient(parameters.Get(RegistrationRequestParameters.ClientId));
            if (client == null) {
                return Error(OidcConstants.AuthorizeErrors.UnauthorizedClient, "Client is unknown or not enabled.");
            }
            if (client.ProtocolType != IdentityServerConstants.ProtocolTypes.OpenIdConnect) {
                return Error(OidcConstants.AuthorizeErrors.UnauthorizedClient, "Invalid protocol.");
            }
            // Validate requested scopes.
            var isOpenIdRequest = false;
            var requestedScopes = parameters.Get(RegistrationRequestParameters.Scope).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (requestedScopes.Contains(IdentityServerConstants.StandardScopes.OpenId)) {
                isOpenIdRequest = true;
            }
            var validatedResources = await ResourceValidator.ValidateRequestedResourcesAsync(new ResourceValidationRequest {
                Client = client,
                Scopes = requestedScopes
            });
            if (!validatedResources.Succeeded) {
                return Error(OidcConstants.AuthorizeErrors.InvalidScope, "Invalid scope.");
            }
            if (validatedResources.Resources.IdentityResources.Any() && !isOpenIdRequest) {
                return Error(OidcConstants.AuthorizeErrors.InvalidScope, "Identity scopes requested, but openid scope is missing.");
            }
            // Finally return result.
            return new DeviceAuthorizationRequestValidationResult {
                IsError = false,
                Client = client,
                CodeChallenge = parameters.Get(RegistrationRequestParameters.CodeChallenge),
                Device = device,
                InteractionMode = InteractionMode.Fingerprint,
                IsOpenIdRequest = isOpenIdRequest,
                RequestedScopes = requestedScopes,
                UserId = device.UserId
            };
        }
    }
}
