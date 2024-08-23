using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.ResponseHandling;
using Indice.AspNetCore.Extensions;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.DeviceAuthentication.Endpoints.Results;
using Indice.Features.Identity.Core.DeviceAuthentication.ResponseHandling;
using Indice.Features.Identity.Core.DeviceAuthentication.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Endpoints;

internal class DeviceAuthenticationEndpoint : IEndpointHandler
{
    public DeviceAuthenticationEndpoint(
        DeviceAuthenticationRequestValidator requestValidator,
        DeviceAuthenticationResponseGenerator responseGenerator,
        ILogger<DeviceAuthenticationEndpoint> logger,
        ExtendedUserManager<User> userManager
    ) {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        Request = requestValidator ?? throw new ArgumentNullException(nameof(requestValidator));
        Response = responseGenerator ?? throw new ArgumentNullException(nameof(responseGenerator));
    }

    public DeviceAuthenticationRequestValidator Request { get; }
    public DeviceAuthenticationResponseGenerator Response { get; }
    public ILogger<DeviceAuthenticationEndpoint> Logger { get; }
    public ExtendedUserManager<User> UserManager { get; }

    public async Task<IEndpointResult> ProcessAsync(HttpContext httpContext) {
        Logger.LogInformation($"[{nameof(DeviceAuthenticationEndpoint)}] Started processing trusted device authorization endpoint.");
        var isPostRequest = HttpMethods.IsPost(httpContext.Request.Method);
        var isApplicationFormContentType = httpContext.Request.HasApplicationFormContentType();
        // Validate HTTP request type and method.
        if (!isPostRequest || !isApplicationFormContentType) {
            return Error(OidcConstants.TokenErrors.InvalidRequest, "Request must be of type 'POST' and have a Content-Type equal to 'application/x-www-form-urlencoded'.");
        }
        // Validate request data and access token.
        var parameters = (await httpContext.Request.ReadFormAsync()).AsNameValueCollection();
        var requestValidationResult = await Request.Validate(parameters);
        if (requestValidationResult.IsError) {
            return Error(requestValidationResult.Error, requestValidationResult.ErrorDescription);
        }
        // Create endpoint response.
        var response = await Response.Generate(requestValidationResult);
        Logger.LogInformation($"[{nameof(DeviceAuthenticationEndpoint)}] Trusted device authorization endpoint success.");
        return new DeviceAuthenticationResult(response);
    }

    private DeviceAuthenticationErrorResult Error(string error, string? errorDescription = null, Dictionary<string, object>? custom = null) {
        var response = new TokenErrorResponse {
            Error = error,
            ErrorDescription = errorDescription,
            Custom = custom
        };
        Logger.LogError("[{EndpointName}] Trusted device authorization endpoint error: {Error}:{ErrorDescription}", nameof(DeviceAuthenticationEndpoint), error, errorDescription ?? " -no message-");
        return new DeviceAuthenticationErrorResult(response);
    }
}
