using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Validation;
using Indice.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Identity.Features
{
    internal class CompleteRegistrationEndpoint : IEndpointHandler
    {
        private readonly BearerTokenUsageValidator _token;
        private readonly CompleteRegistrationRequestValidator _request;
        private readonly ILogger<CompleteRegistrationEndpoint> _logger;

        public CompleteRegistrationEndpoint(
            BearerTokenUsageValidator tokenUsageValidator,
            CompleteRegistrationRequestValidator requestValidator,
            ILogger<CompleteRegistrationEndpoint> logger
        ) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _request = requestValidator ?? throw new ArgumentNullException(nameof(requestValidator));
            _token = tokenUsageValidator ?? throw new ArgumentNullException(nameof(tokenUsageValidator));
        }

        public async Task<IEndpointResult> ProcessAsync(HttpContext httpContext) {
            _logger.LogInformation($"[{nameof(CompleteRegistrationEndpoint)}] Started processing trusted device registration endpoint.");
            var isPostRequest = HttpMethods.IsPost(httpContext.Request.Method);
            var isApplicationFormContentType = httpContext.Request.HasApplicationFormContentType();
            // Validate HTTP request type.
            if (!isPostRequest || !isApplicationFormContentType) {
                return Error(OidcConstants.TokenErrors.InvalidRequest, "Request must be of type 'POST' and have a Content-Type equal to 'application/x-www-form-urlencoded'.");
            }
            // Ensure that a valid 'Authorization' header exists.
            var tokenUsageResult = await _token.Validate(httpContext);
            if (!tokenUsageResult.TokenFound) {
                return Error(OidcConstants.ProtectedResourceErrors.InvalidToken, "No access token is present in the request.");
            }
            // Validate request data and access token.
            var parameters = (await httpContext.Request.ReadFormAsync()).AsNameValueCollection();
            var requestValidationResult = await _request.Validate(tokenUsageResult.Token, parameters);
            if (requestValidationResult.IsError) {
                return Error(requestValidationResult.Error, requestValidationResult.ErrorDescription);
            }
            throw new NotImplementedException();
        }

        private AuthorizationErrorResult Error(string error, string errorDescription = null, Dictionary<string, object> custom = null) {
            var response = new TokenErrorResponse {
                Error = error,
                ErrorDescription = errorDescription,
                Custom = custom
            };
            _logger.LogError("[CompleteRegistrationEndpoint] Trusted device authorization endpoint error: {Error}:{ErrorDescription}", error, errorDescription ?? "-no message-");
            return new AuthorizationErrorResult(response);
        }
    }
}
