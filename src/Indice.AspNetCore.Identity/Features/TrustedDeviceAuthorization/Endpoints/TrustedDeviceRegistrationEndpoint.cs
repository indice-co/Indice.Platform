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
    /// <summary>
    /// 
    /// </summary>
    internal class TrustedDeviceRegistrationEndpoint : IEndpointHandler
    {
        private readonly BearerTokenUsageValidator _tokenUsageValidator;
        private readonly ILogger<TrustedDeviceRegistrationEndpoint> _logger;
        private readonly ITrustedDeviceRegistrationRequestValidator _requestValidator;
        private readonly ITrustedDeviceRegistrationResponseGenerator _responseGenerator;

        public TrustedDeviceRegistrationEndpoint(
            BearerTokenUsageValidator tokenUsageValidator,
            ILogger<TrustedDeviceRegistrationEndpoint> logger,
            ITrustedDeviceRegistrationRequestValidator requestValidator,
            ITrustedDeviceRegistrationResponseGenerator responseGenerator
        ) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _requestValidator = requestValidator ?? throw new ArgumentNullException(nameof(requestValidator));
            _responseGenerator = responseGenerator ?? throw new ArgumentNullException(nameof(responseGenerator));
            _tokenUsageValidator = tokenUsageValidator ?? throw new ArgumentNullException(nameof(tokenUsageValidator));
        }

        /// <inheritdoc />
        public async Task<IEndpointResult> ProcessAsync(HttpContext context) {
            _logger.LogDebug("Started processing trusted device registration endpoint.");
            var isPostRequest = HttpMethods.IsPost(context.Request.Method);
            var isApplicationFormContentType = context.Request.HasApplicationFormContentType();
            // Validate HTTP request type.
            if (!isPostRequest || !isApplicationFormContentType) {
                _logger.LogWarning("Invalid HTTP request for trusted device registration endpoint.");
                return Error(OidcConstants.TokenErrors.InvalidRequest);
            }
            // Ensure that a valid 'Authorization' header exists.
            var tokenUsageResult = await _tokenUsageValidator.ValidateAsync(context);
            if (!tokenUsageResult.TokenFound) {
                _logger.LogError("No access token found.");
                return Error(OidcConstants.ProtectedResourceErrors.InvalidToken);
            }
            // Validate request data.
            var form = (await context.Request.ReadFormAsync()).AsNameValueCollection();
            var requestValidationResult = await _requestValidator.ValidateAsync(form);
            if (requestValidationResult.IsError) {
                return Error(requestValidationResult.Error, requestValidationResult.ErrorDescription);
            }
            // Create application response.
            var response = await _responseGenerator.ProcessAsync(requestValidationResult);
            _logger.LogDebug("Trusted device authorization endpoint success.");
            return new TrustedDeviceRegistrationResult(response);
        }

        private TrustedDeviceAuthorizationErrorResult Error(string error, string errorDescription = null, Dictionary<string, object> custom = null) {
            var response = new TokenErrorResponse {
                Error = error,
                ErrorDescription = errorDescription,
                Custom = custom
            };
            _logger.LogError("Trusted device authorization endpoint error: {error}:{errorDescription}", error, errorDescription ?? "-no message-");
            return new TrustedDeviceAuthorizationErrorResult(response);
        }
    }
}
