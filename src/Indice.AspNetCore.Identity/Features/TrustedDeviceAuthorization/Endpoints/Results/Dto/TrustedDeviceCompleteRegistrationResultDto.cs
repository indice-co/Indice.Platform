using System;
using System.Text.Json.Serialization;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Endpoints.Results
{
    internal class TrustedDeviceCompleteRegistrationResultDto
    {
        public TrustedDeviceCompleteRegistrationResultDto(Guid registrationId) {
            RegistrationId = registrationId;
        }

        [JsonPropertyName("regId")]
        public Guid RegistrationId { get; }
    }
}
