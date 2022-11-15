using System;
using System.Text.Json.Serialization;

namespace Indice.AspNetCore.Identity.DeviceAuthentication.Endpoints.Results
{
    internal class CompleteRegistrationResultDto
    {
        public CompleteRegistrationResultDto(Guid registrationId) {
            RegistrationId = registrationId;
        }

        [JsonPropertyName("registrationId")]
        public Guid RegistrationId { get; }
    }
}
