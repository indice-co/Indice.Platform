using System.Collections.Specialized;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.DeviceAuthentication.Configuration;
using Indice.Features.Identity.Core.DeviceAuthentication.Stores;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Validation;

internal class DeviceAuthenticationRequestValidator : RequestValidatorBase<DeviceAuthenticationRequestValidationResult>
{
    public DeviceAuthenticationRequestValidator(
        IClientStore clientStore,
        ILogger<DeviceAuthenticationRequestValidator> logger,
        IResourceValidator resourceValidator,
        ITokenValidator tokenValidator,
        IUserDeviceStore userDeviceStore,
        ExtendedUserManager<User> userManager
    ) : base(clientStore, tokenValidator) {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        UserDeviceStore = userDeviceStore ?? throw new ArgumentNullException(nameof(userDeviceStore));
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        ResourceValidator = resourceValidator ?? throw new ArgumentNullException(nameof(resourceValidator));
    }

    public ILogger<DeviceAuthenticationRequestValidator> Logger { get; }
    public IResourceValidator ResourceValidator { get; }
    public IUserDeviceStore UserDeviceStore { get; }
    public ExtendedUserManager<User> UserManager { get; }

    public async override Task<DeviceAuthenticationRequestValidationResult> Validate(NameValueCollection parameters, string accessToken = null) {
        Logger.LogDebug($"{nameof(DeviceAuthenticationRequestValidator)}: Started trusted device authorization request validation.");
        // Validate that the consumer specified all required parameters.
        var parametersToValidate = new[] {
            RegistrationRequestParameters.ClientId,
            RegistrationRequestParameters.CodeChallenge,
            RegistrationRequestParameters.RegistrationId,
            RegistrationRequestParameters.Scope
        };
        foreach (var parameter in parametersToValidate) {
            var parameterValue = parameters.Get(parameter);
            if (string.IsNullOrWhiteSpace(parameterValue)) {
                return Error(OidcConstants.TokenErrors.InvalidRequest, $"Parameter '{parameter}' is not specified.");
            }
        }
        // Load device.
        var isValidRegistrationId = Guid.TryParse(parameters.Get(RegistrationRequestParameters.RegistrationId), out var registrationId);
        if (!isValidRegistrationId) {
            return Error(OidcConstants.TokenErrors.InvalidRequest, "Device registration id is not valid.");
        }
        var device = await UserDeviceStore.GetById(registrationId);
        if (device is null || !device.SupportsFingerprintLogin) {
            return Error(OidcConstants.TokenErrors.InvalidRequest, "Device cannot initiate fingerprint login.");
        }
        // Load and validate client.
        var client = await LoadClient(parameters.Get(RegistrationRequestParameters.ClientId));
        if (client is null) {
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
        return new DeviceAuthenticationRequestValidationResult {
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
