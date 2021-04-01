using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Validation;
using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Identity.Models;
using Indice.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Identity.Features
{
    internal class TrustedDeviceRegistrationEndpoint : IEndpointHandler
    {
        private readonly BearerTokenUsageValidator _token;
        private readonly ILogger<TrustedDeviceRegistrationEndpoint> _logger;
        private readonly ITrustedDeviceRegistrationRequestValidator _request;
        private readonly ITrustedDeviceRegistrationResponseGenerator _response;
        private readonly ITotpService _totpService;
        private readonly ExtendedIdentityDbContext<User, Role> _dbContext;

        public TrustedDeviceRegistrationEndpoint(
            BearerTokenUsageValidator tokenUsageValidator,
            ILogger<TrustedDeviceRegistrationEndpoint> logger,
            ITrustedDeviceRegistrationRequestValidator requestValidator,
            ITrustedDeviceRegistrationResponseGenerator responseGenerator,
            ITotpService totpService,
            ExtendedIdentityDbContext<User, Role> dbContext
        ) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _request = requestValidator ?? throw new ArgumentNullException(nameof(requestValidator));
            _response = responseGenerator ?? throw new ArgumentNullException(nameof(responseGenerator));
            _totpService = totpService ?? throw new ArgumentNullException(nameof(totpService));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _token = tokenUsageValidator ?? throw new ArgumentNullException(nameof(tokenUsageValidator));
        }

        public async Task<IEndpointResult> ProcessAsync(HttpContext context) {
            _logger.LogDebug("Started processing trusted device registration endpoint");
            var isPostRequest = HttpMethods.IsPost(context.Request.Method);
            var isApplicationFormContentType = context.Request.HasApplicationFormContentType();
            // Validate HTTP request type.
            if (!isPostRequest || !isApplicationFormContentType) {
                _logger.LogWarning("Invalid HTTP request for trusted device registration endpoint");
                return Error(OidcConstants.TokenErrors.InvalidRequest);
            }
            // Ensure that a valid 'Authorization' header exists.
            var tokenUsageResult = await _token.Validate(context);
            if (!tokenUsageResult.TokenFound) {
                _logger.LogError("No access token found");
                return Error(OidcConstants.ProtectedResourceErrors.InvalidToken);
            }
            // Validate request data.
            var parameters = (await context.Request.ReadFormAsync()).AsNameValueCollection();
            var requestValidationResult = await _request.Validate(tokenUsageResult.Token, parameters);
            if (requestValidationResult.IsError) {
                return Error(requestValidationResult.Error, requestValidationResult.ErrorDescription);
            }
            // Ensure device is not already registered or belongs to any other user.
            var existingDevice = await _dbContext.UserDevices.SingleOrDefaultAsync(x => x.DeviceId == requestValidationResult.DeviceId);
            if (existingDevice != null) {
                _logger.LogError("Device is already registered.");
                return Error(OidcConstants.ProtectedResourceErrors.InvalidToken);
            }
            // Send OTP code.
            var totpResult = await _totpService.Send(message =>
                message.UsePrincipal(requestValidationResult.Principal)
                       .WithMessage("Device registration OTP code is {0}.")
                       .UsingSms()
                       .WithPurpose($"device-registration:{requestValidationResult.UserId}:{requestValidationResult.DeviceId}")
            );
            if (!totpResult.Success) {
                return Error(totpResult.Error);
            }
            // Create application response.
            var response = await _response.Generate(requestValidationResult);
            _logger.LogDebug("Trusted device authorization endpoint success");
            return new TrustedDeviceRegistrationResult(response);
        }

        private TrustedDeviceAuthorizationErrorResult Error(string error, string errorDescription = null, Dictionary<string, object> custom = null) {
            var response = new TokenErrorResponse {
                Error = error,
                ErrorDescription = errorDescription,
                Custom = custom
            };
            _logger.LogError("Trusted device authorization endpoint error: {Error}:{ErrorDescription}", error, errorDescription ?? "-no message-");
            return new TrustedDeviceAuthorizationErrorResult(response);
        }
    }
}
