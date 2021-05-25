using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.ResponseHandling;
using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Endpoints.Results;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.ResponseHandling;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Stores;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Endpoints
{
    internal class CompleteRegistrationEndpoint : IEndpointHandler
    {
        public CompleteRegistrationEndpoint(
            BearerTokenUsageValidator tokenUsageValidator,
            CompleteRegistrationRequestValidator requestValidator,
            CompleteRegistrationResponseGenerator responseGenerator,
            ILogger<CompleteRegistrationEndpoint> logger,
            IUserDeviceStore userDeviceStore
        ) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            UserDeviceStore = userDeviceStore ?? throw new ArgumentNullException(nameof(userDeviceStore));
            Request = requestValidator ?? throw new ArgumentNullException(nameof(requestValidator));
            Response = responseGenerator ?? throw new ArgumentNullException(nameof(responseGenerator));
            Token = tokenUsageValidator ?? throw new ArgumentNullException(nameof(tokenUsageValidator));
        }

        public BearerTokenUsageValidator Token { get; }
        public CompleteRegistrationRequestValidator Request { get; }
        public CompleteRegistrationResponseGenerator Response { get; }
        public ILogger<CompleteRegistrationEndpoint> Logger { get; }
        public IUserDeviceStore UserDeviceStore { get; }

        public async Task<IEndpointResult> ProcessAsync(HttpContext httpContext) {
            Logger.LogInformation($"[{nameof(CompleteRegistrationEndpoint)}] Started processing trusted device registration endpoint.");
            var isPostRequest = HttpMethods.IsPost(httpContext.Request.Method);
            var isApplicationFormContentType = httpContext.Request.HasApplicationFormContentType();
            // Validate HTTP request type.
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
            // Get device that is operating, if any.
            var existingDevice = await UserDeviceStore.GetByDeviceId(requestValidationResult.DeviceId);
            var isNewDeviceOrOwnedByUser = existingDevice == null || existingDevice.UserId.Equals(requestValidationResult.UserId, StringComparison.OrdinalIgnoreCase);
            if (!isNewDeviceOrOwnedByUser) {
                return Error(OidcConstants.ProtectedResourceErrors.InvalidToken, "Device does not belong to the this user.");
            }
            requestValidationResult.SetDevice(existingDevice);
            // Create endpoint response.
            var response = await Response.Generate(requestValidationResult);
            Logger.LogInformation($"[{nameof(InitRegistrationEndpoint)}] Trusted device authorization endpoint success.");
            return new CompleteRegistrationResult(response);
        }

        private AuthorizationErrorResult Error(string error, string errorDescription = null, Dictionary<string, object> custom = null) {
            var response = new TokenErrorResponse {
                Error = error,
                ErrorDescription = errorDescription,
                Custom = custom
            };
            Logger.LogError("[{EndpointName}] Trusted device authorization endpoint error: {Error}:{ErrorDescription}", nameof(CompleteRegistrationEndpoint), error, errorDescription ?? "-no message-");
            return new AuthorizationErrorResult(response);
        }
    }
}
