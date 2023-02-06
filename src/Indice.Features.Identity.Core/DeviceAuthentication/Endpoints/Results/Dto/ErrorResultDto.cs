using System.Text.Json.Serialization;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Endpoints.Results
{
    internal class ErrorResultDto
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }
        [JsonPropertyName("error_description")]
        public string ErrorDescription { get; set; }
        [JsonExtensionData]
        [JsonPropertyName("custom")]
        public Dictionary<string, object> Custom { get; set; }
    }
}
