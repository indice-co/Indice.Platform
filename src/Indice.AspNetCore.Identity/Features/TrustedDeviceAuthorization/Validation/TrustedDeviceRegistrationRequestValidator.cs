using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Identity.Features
{
    internal class TrustedDeviceRegistrationRequestValidator : ITrustedDeviceRegistrationRequestValidator
    {
        private readonly ILogger<TrustedDeviceRegistrationRequestValidator> _logger;

        public TrustedDeviceRegistrationRequestValidator(ILogger<TrustedDeviceRegistrationRequestValidator> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<TrustedDeviceRegistrationRequestValidationResult> ValidateAsync(NameValueCollection parameters) {
            _logger.LogDebug("Started trusted device registration request validation.");
            var validatedRequest = new ValidatedTrustedDeviceRegistrationRequest {
                Raw = parameters ?? throw new ArgumentNullException(nameof(parameters))
            };
            return Task.FromResult((TrustedDeviceRegistrationRequestValidationResult)null);
        }
    }
}
