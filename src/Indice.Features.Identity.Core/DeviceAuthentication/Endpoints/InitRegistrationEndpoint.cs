using IdentityModel;
using IdentityServer4;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Indice.AspNetCore.Extensions;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.DeviceAuthentication.Configuration;
using Indice.Features.Identity.Core.DeviceAuthentication.Endpoints.Results;
using Indice.Features.Identity.Core.DeviceAuthentication.ResponseHandling;
using Indice.Features.Identity.Core.DeviceAuthentication.Stores;
using Indice.Features.Identity.Core.DeviceAuthentication.Validation;
using Indice.Features.Identity.Core.Totp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Endpoints;

internal class InitRegistrationEndpoint : IEndpointHandler
{
    public InitRegistrationEndpoint(
        BearerTokenUsageValidator tokenUsageValidator,
        ILogger<InitRegistrationEndpoint> logger,
        InitRegistrationRequestValidator requestValidator,
        InitRegistrationResponseGenerator responseGenerator,
        IProfileService profileService,
        IResourceStore resourceStore,
        TotpServiceFactory totpServiceFactory,
        IUserDeviceStore userDeviceStore,
        IdentityMessageDescriber identityMessageDescriber,
        IOptions<DeviceAuthenticationOptions> deviceAuthenticationOptions
    ) {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ProfileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        Request = requestValidator ?? throw new ArgumentNullException(nameof(requestValidator));
        ResourceStore = resourceStore;
        Response = responseGenerator ?? throw new ArgumentNullException(nameof(responseGenerator));
        Token = tokenUsageValidator ?? throw new ArgumentNullException(nameof(tokenUsageValidator));
        TotpServiceFactory = totpServiceFactory ?? throw new ArgumentNullException(nameof(totpServiceFactory));
        UserDeviceStore = userDeviceStore ?? throw new ArgumentNullException(nameof(userDeviceStore));
        IdentityMessageDescriber = identityMessageDescriber ?? throw new ArgumentNullException(nameof(identityMessageDescriber));
        DeviceAuthenticationOptions = deviceAuthenticationOptions?.Value ?? throw new ArgumentNullException(nameof(deviceAuthenticationOptions));
    }

    public BearerTokenUsageValidator Token { get; }
    public ILogger<InitRegistrationEndpoint> Logger { get; }
    public InitRegistrationRequestValidator Request { get; }
    public InitRegistrationResponseGenerator Response { get; }
    public IProfileService ProfileService { get; }
    public IResourceStore ResourceStore { get; }
    public TotpServiceFactory TotpServiceFactory { get; }
    public IUserDeviceStore UserDeviceStore { get; }
    public IdentityMessageDescriber IdentityMessageDescriber { get; }
    public DeviceAuthenticationOptions DeviceAuthenticationOptions { get; }

    public async Task<IEndpointResult> ProcessAsync(HttpContext httpContext) {
        Logger.LogInformation($"[{nameof(InitRegistrationEndpoint)}] Started processing trusted device registration initiation endpoint.");
        var isPostRequest = HttpMethods.IsPost(httpContext.Request.Method);
        var isApplicationFormContentType = httpContext.Request.HasApplicationFormContentType();
        // Validate HTTP request type and method.
        if (!isPostRequest || !isApplicationFormContentType) {
            return Error(OidcConstants.TokenErrors.InvalidRequest, "Request must be of type 'POST' and have a Content-Type equal to 'application/x-www-form-urlencoded'.");
        }
        // Ensure that a valid 'Authorization' header exists.
        var tokenUsageResult = await Token.Validate(httpContext);
        if (!tokenUsageResult.TokenFound) {
            return Error(OidcConstants.ProtectedResourceErrors.InvalidToken, "No access token is present in the request.");
        }
        // Validate request data and access token.
        var parameters = (await httpContext.Request.ReadFormAsync()).AsNameValueCollection();
        var requestValidationResult = await Request.Validate(parameters, tokenUsageResult.Token);
        if (requestValidationResult.IsError) {
            return Error(requestValidationResult.Error, requestValidationResult.ErrorDescription);
        }
        // Ensure device is not already registered or belongs to any other user.
        var existingDevice = await UserDeviceStore.GetByDeviceId(requestValidationResult.DeviceId);
        var isNewDeviceOrOwnedByUser = existingDevice == null || existingDevice.UserId.Equals(requestValidationResult.UserId, StringComparison.OrdinalIgnoreCase);
        if (!isNewDeviceOrOwnedByUser) {
            return Error(OidcConstants.ProtectedResourceErrors.InvalidToken, "Device does not belong to the this user.");
        }
        // Ensure that the principal has declared a phone number which is also confirmed.
        // We will get these 2 claims by retrieving the identity resources from the store (using the requested scopes existing in the access token) and then calling the profile service.
        // This will help us make sure that the 'phone' scope was requested and finally allowed in the token endpoint.
        var identityResources = await ResourceStore.FindEnabledIdentityResourcesByScopeAsync(requestValidationResult.RequestedScopes);
        var resources = new Resources(identityResources, Enumerable.Empty<ApiResource>(), Enumerable.Empty<ApiScope>());
        var validatedResources = new ResourceValidationResult(resources);
        if (!validatedResources.Succeeded) {
            return Error(OidcConstants.ProtectedResourceErrors.InvalidToken, "Identity resources could be validated.");
        }
        var requestedClaimTypes = resources.IdentityResources.SelectMany(x => x.UserClaims).Distinct();
        var profileDataRequestContext = new ProfileDataRequestContext(requestValidationResult.Principal, requestValidationResult.Client, IdentityServerConstants.ProfileDataCallers.UserInfoEndpoint, requestedClaimTypes) {
            RequestedResources = validatedResources
        };
        var phoneNumberClaim = profileDataRequestContext.Subject.FindFirst(JwtClaimTypes.PhoneNumber);
        var phoneNumberVerifiedClaim = profileDataRequestContext.Subject.FindFirst(JwtClaimTypes.PhoneNumberVerified);
        if (string.IsNullOrWhiteSpace(phoneNumberClaim?.Value)
         || phoneNumberVerifiedClaim == null
         || (bool.TryParse(phoneNumberVerifiedClaim.Value, out var phoneNumberVerified) && !phoneNumberVerified)) {
            return Error(OidcConstants.ProtectedResourceErrors.InvalidToken, "User does not have a phone number or the phone number is not verified.");
        }
        var amrClaim = profileDataRequestContext.Subject.FindFirst(JwtClaimTypes.AuthenticationMethod);
        var mfaPassed = amrClaim is not null && amrClaim.Value == CustomGrantTypes.Mfa;
        if (DeviceAuthenticationOptions.AlwaysSendOtp || !mfaPassed) {
            // Send OTP code.
            var totpResult = await TotpServiceFactory.Create<User>().SendAsync(totp => totp
                .ToPrincipal(requestValidationResult.Principal)
                .WithMessage(IdentityMessageDescriber.DeviceRegistrationCodeMessage(existingDevice?.Name, requestValidationResult.InteractionMode))
                .UsingDeliveryChannel(requestValidationResult.DeliveryChannel)
                .WithPurpose(Constants.DeviceAuthenticationOtpPurpose(requestValidationResult.UserId, requestValidationResult.DeviceId))
            );
            if (!totpResult.Success) {
                return Error(totpResult.Error);
            }
        }
        // Create endpoint response.
        var response = await Response.Generate(requestValidationResult);
        Logger.LogInformation($"[{nameof(InitRegistrationEndpoint)}] Trusted device registration initiation endpoint success.");
        return new InitRegistrationResult(response);
    }

    private DeviceAuthenticationErrorResult Error(string error, string errorDescription = null, Dictionary<string, object> custom = null) {
        var response = new TokenErrorResponse {
            Error = error,
            ErrorDescription = errorDescription,
            Custom = custom
        };
        Logger.LogError("[{EndpointName}] Trusted device registration initiation endpoint error: {Error}:{ErrorDescription}", nameof(InitRegistrationEndpoint), error, errorDescription ?? " -no message-");
        return new DeviceAuthenticationErrorResult(response);
    }
}
