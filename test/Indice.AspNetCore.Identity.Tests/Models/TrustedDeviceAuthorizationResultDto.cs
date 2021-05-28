using System.Text.Json.Serialization;

namespace Indice.AspNetCore.Identity.Tests.Models
{
    public class TrustedDeviceAuthorizationResultDto
    {
        [JsonPropertyName("challenge")]
        public string Challenge { get; set; }
    }
}
