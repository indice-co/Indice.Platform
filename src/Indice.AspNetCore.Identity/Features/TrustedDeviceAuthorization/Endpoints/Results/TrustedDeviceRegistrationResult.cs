using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Identity.Features
{
    internal class TrustedDeviceRegistrationResult : IEndpointResult
    {
        public TrustedDeviceRegistrationResult(TrustedDeviceRegistrationResponse response) {
            Response = response ?? throw new ArgumentNullException(nameof(response));
        }

        public TrustedDeviceRegistrationResponse Response { get; }

        public async Task ExecuteAsync(HttpContext context) {
            context.Response.SetNoCache();
            var result = new TrustedDeviceAuthorizationResultDto(Response.UserId, Response.DeviceFriendlyName, Response.Challenge);
            await context.Response.WriteJsonAsync(result);
        }
    }

    internal class TrustedDeviceAuthorizationResultDto
    {
        public TrustedDeviceAuthorizationResultDto(string userId, string deviceFriendlyName, byte[] challenge) {
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
            DeviceFriendlyName = deviceFriendlyName ?? throw new ArgumentNullException(nameof(deviceFriendlyName));
            Challenge = challenge != null ? Convert.ToBase64String(challenge) : throw new ArgumentNullException(nameof(challenge));
        }

        [JsonPropertyName("user_id")]
        public string UserId { get; }
        [JsonPropertyName("device_friendly_name")]
        public string DeviceFriendlyName { get; }
        [JsonPropertyName("challenge")]
        public string Challenge { get; }
    }
}
