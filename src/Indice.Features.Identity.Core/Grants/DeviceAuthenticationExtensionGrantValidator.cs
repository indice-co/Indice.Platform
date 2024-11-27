using System.Security.Claims;
using System.Text.Json;
using IdentityServer4;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Indice.AspNetCore.Extensions;
using Indice.Extensions;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.DeviceAuthentication.Configuration;
using Indice.Features.Identity.Core.DeviceAuthentication.Models;
using Indice.Features.Identity.Core.DeviceAuthentication.Services;
using Indice.Features.Identity.Core.DeviceAuthentication.Stores;
using Indice.Features.Identity.Core.DeviceAuthentication.Validation;
using Indice.Features.Identity.Core.Events;
using Indice.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Indice.Features.Identity.Core.Grants;

internal class DeviceAuthenticationExtensionGrantValidator(
    IDeviceAuthenticationCodeChallengeStore codeChallengeStore,
    IDevicePasswordHasher devicePasswordHasher,
    IUserDeviceStore userDeviceStore,
    ExtendedUserManager<User> userManager,
    IEventService eventService,
    IHttpContextAccessor httpContextAccessor) : RequestChallengeValidator, IExtensionGrantValidator
{
    public string GrantType => CustomGrantTypes.DeviceAuthentication;
    public IDeviceAuthenticationCodeChallengeStore CodeChallengeStore { get; } = codeChallengeStore ?? throw new ArgumentNullException(nameof(codeChallengeStore));
    public IDevicePasswordHasher DevicePasswordHasher { get; } = devicePasswordHasher ?? throw new ArgumentNullException(nameof(devicePasswordHasher));

    /// <summary>Gets the current time, primarily for unit testing.</summary>
    protected TimeProvider TimeProvider { get; private set; } = TimeProvider.System;
    public IUserDeviceStore UserDeviceStore { get; } = userDeviceStore ?? throw new ArgumentNullException(nameof(userDeviceStore));
    public ExtendedUserManager<User> UserManager { get; } = userManager ?? throw new ArgumentNullException(nameof(userManager));
    public IEventService EventService { get; } = eventService ?? throw new ArgumentNullException(nameof(eventService));
    public IHttpContextAccessor HttpContextAccessor { get; } = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

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
        var ip = HttpContextAccessor.HttpContext.GetClientIpAddress();
        var claims = new List<Claim>();
        if (ip is not null) {
            claims.Add(new Claim(BasicClaimTypes.IPAddress, ip.ToString()));
        }
        if (!string.IsNullOrWhiteSpace(device.DeviceId)) {
            claims.Add(new Claim(BasicClaimTypes.DeviceId, device.DeviceId));
        }
        if (parameters.Get(BasicClaimTypes.AuthorizationDetails) is { } authorizationDetails) {
            var validateAuthorizationDetails = ValidateAuthorizationDetails(authorizationDetails);
            if (validateAuthorizationDetails.IsError) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest, validateAuthorizationDetails.ErrorDescription) {
                    Error = ExtraTokenRequestErrors.InvalidAuthorizationDetails
                };
                return;
            }

            claims.Add(new Claim(BasicClaimTypes.AuthorizationDetails, authorizationDetails, IdentityServerConstants.ClaimValueTypes.Json));
        }
        // If code is present we are heading towards fingerprint login.
        if (hasCode) {
            // Retrieve authorization code from the store.
            var authorizationCode = await CodeChallengeStore.GetDeviceAuthenticationCode(code!);
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
            var codeVerifier = parameters.Get(RegistrationRequestParameters.CodeVerifier)!;
            var authorizationCodeValidationResult = await ValidateAuthorizationCode(code!, authorizationCode, codeVerifier, registrationId, context.Request.Client);
            if (authorizationCodeValidationResult.IsError) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, authorizationCodeValidationResult.ErrorDescription);
                return;
            }
            // Validate given public key against signature for fingerprint.
            var publicKey = parameters.Get(RegistrationRequestParameters.PublicKey)!;
            var codeSignature = parameters.Get(RegistrationRequestParameters.CodeSignature)!;
            var publicKeyValidationResult = ValidateSignature(publicKey, code!, codeSignature);
            if (publicKeyValidationResult.IsError) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, publicKeyValidationResult.ErrorDescription);
                return;
            }
            await UserDeviceStore.UpdatePublicKey(device, publicKey);
            // Grant access token.
            context.Result = new GrantValidationResult(authorizationCode.Subject.GetSubjectId(), GrantType, claims: claims);
        }
        // If pin is present we are heading towards a 4-Pin login.
        if (hasPin) {
            var result = DevicePasswordHasher.VerifyHashedPassword(device, device.Password!, pin!);
            if (result == PasswordVerificationResult.Failed) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Wrong pin.");
                return;
            }
            context.Result = new GrantValidationResult(device.UserId, GrantType, claims: claims);
        }
        await UserDeviceStore.UpdateLastSignInDate(device);
        await UserManager.SetLastSignInDateAsync(user, DateTimeOffset.UtcNow);
        if (context.Result.IsError) {
            await RaiseUserLoginFailureEvent(user, context);
        } else {
            await RaiseUserLoginSuccessEvent(user, context);
        }
    }

    private Task RaiseUserLoginSuccessEvent(User user, ExtensionGrantValidationContext context) => EventService.RaiseAsync(new ExtendedUserLoginSuccessEvent(
        user!.UserName!,
        user.Id,
        user!.UserName!,
        clientId: context.Request.ClientId,
        clientName: context.Request.Client.ClientName,
        authenticationMethods: [context.Result.Subject.Identity?.AuthenticationType!]
    ));

    private Task RaiseUserLoginFailureEvent(User user, ExtensionGrantValidationContext context) => EventService.RaiseAsync(new ExtendedUserLoginFailureEvent(
        user!.UserName!,
        "Biometric login failure.",
        clientId: context.Request.ClientId,
        subjectId: user.Id
    ));

    private async Task<ValidationResult> ValidateAuthorizationCode(string code, DeviceAuthenticationCode authorizationCode, string codeVerifier, Guid registrationId, Client client) {
        // Validate that the current client is not trying to use an authorization code of a different client.
        if (authorizationCode.ClientId != client.ClientId) {
            return Invalid("Authorization code is invalid.");
        }
        // Validate that the current device is not trying to use an authorization code of a different device.
        if (Guid.Parse(authorizationCode.DeviceId!) != registrationId) {
            return Invalid("Authorization code is invalid.");
        }
        // Remove authorization code.
        await CodeChallengeStore.RemoveDeviceAuthenticationCode(code);
        // Validate code expiration.
        if (authorizationCode.CreationTime.HasExceeded(authorizationCode.Lifetime, TimeProvider.GetUtcNow().UtcDateTime)) {
            return Invalid("Authorization code is invalid.");
        }
        if (authorizationCode.CreationTime.HasExceeded(client.AuthorizationCodeLifetime, TimeProvider.GetUtcNow().UtcDateTime)) {
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

    /// <summary>
    /// Validate the RFC-9396 authorization_details request, that contains the property type
    /// <list type="bullet">
    /// <item>contains an unknown authorization details type value,</item>
    /// <item>is an object of known type but containing unknown fields,</item>
    /// <item>contains fields of the wrong type for the authorization details type,</item>
    /// <item>contains fields with invalid values for the authorization details type, or</item>
    /// <item>is missing required fields for the authorization details type</item>
    /// </list>
    /// </summary>
    /// <remarks>https://datatracker.ietf.org/doc/html/rfc9396</remarks>
    /// <returns></returns>
    private ValidationResult ValidateAuthorizationDetails(string authorizationDetails) {
        JsonDocument authorizationDetailsJson;
        try {
            authorizationDetailsJson = JsonDocument.Parse(authorizationDetails);
        } catch (Exception) {
            return Invalid("Authorization details is invalid. Invalid json.");
        }

        var ok = authorizationDetailsJson.RootElement.ValueKind is JsonValueKind.Object
            ? authorizationDetailsJson.RootElement.TryGetProperty("type", out _)
            : authorizationDetailsJson.RootElement.EnumerateArray().All(x => x.TryGetProperty("type", out _));

        return ok
            ? Success()
            : Invalid("Authorization details is invalid. Unknown type.");
    }
}
