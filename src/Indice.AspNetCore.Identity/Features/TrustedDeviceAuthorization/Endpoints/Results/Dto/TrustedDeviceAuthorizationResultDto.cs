using System;
using System.Text.Json.Serialization;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Endpoints.Results
{
    internal class TrustedDeviceAuthorizationResultDto
    {
        public TrustedDeviceAuthorizationResultDto(string challenge) {
            Challenge = challenge ?? throw new ArgumentNullException(nameof(challenge));
        }

        [JsonPropertyName("challenge")]
        public string Challenge { get; }
    }
}
