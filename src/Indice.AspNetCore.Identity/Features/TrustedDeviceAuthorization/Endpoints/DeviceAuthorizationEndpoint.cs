using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.ResponseHandling;
using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Endpoints.Results;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using DeviceAuthorizationResponseGenerator = Indice.AspNetCore.Identity.TrustedDeviceAuthorization.ResponseHandling.DeviceAuthorizationResponseGenerator;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Endpoints
{
    internal class DeviceAuthorizationEndpoint : IEndpointHandler
    {
        public DeviceAuthorizationEndpoint(
            DeviceAuthorizationRequestValidator requestValidator,
            DeviceAuthorizationResponseGenerator responseGenerator,
            ILogger<DeviceAuthorizationEndpoint> logger
        ) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Request = requestValidator ?? throw new ArgumentNullException(nameof(requestValidator));
            Response = responseGenerator ?? throw new ArgumentNullException(nameof(responseGenerator));
        }

        public DeviceAuthorizationRequestValidator Request { get; }
        public DeviceAuthorizationResponseGenerator Response { get; }
        public ILogger<DeviceAuthorizationEndpoint> Logger { get; }

        public async Task<IEndpointResult> ProcessAsync(HttpContext httpContext) {
            Logger.LogInformation($"[{nameof(DeviceAuthorizationEndpoint)}] Started processing trusted device authorization endpoint.");
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
            Logger.LogInformation($"[{nameof(DeviceAuthorizationEndpoint)}] Trusted device authorization endpoint success.");
            return new DeviceAuthorizationResult(response);
        }

        private AuthorizationErrorResult Error(string error, string errorDescription = null, Dictionary<string, object> custom = null) {
            var response = new TokenErrorResponse {
                Error = error,
                ErrorDescription = errorDescription,
                Custom = custom
            };
            Logger.LogError("[{EndpointName}] Trusted device authorization endpoint error: {Error}:{ErrorDescription}", nameof(DeviceAuthorizationEndpoint), error, errorDescription ?? " -no message-");
            return new AuthorizationErrorResult(response);
        }
    }
}
