using System;
using System.Text.Json.Serialization;

namespace Indice.AspNetCore.Identity.DeviceAuthentication.Endpoints.Results
{
    internal class DeviceAuthenticationResultDto
    {
        public DeviceAuthenticationResultDto(string challenge) {
            Challenge = challenge ?? throw new ArgumentNullException(nameof(challenge));
        }

        [JsonPropertyName("challenge")]
        public string Challenge { get; }
    }
}
