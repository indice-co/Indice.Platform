using System.Text.Json.Serialization;

namespace Indice.Features.Identity.Tests.Models
{
    public class TrustedDeviceAuthorizationResultDto
    {
        [JsonPropertyName("challenge")]
        public string Challenge { get; set; }
    }
}
