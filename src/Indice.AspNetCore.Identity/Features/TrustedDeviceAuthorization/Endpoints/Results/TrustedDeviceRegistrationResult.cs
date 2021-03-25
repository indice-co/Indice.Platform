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
            var result = new TrustedDeviceAuthorizationResultDto(Response.Challenge);
            await context.Response.WriteJsonAsync(result);
        }
    }

    internal class TrustedDeviceAuthorizationResultDto
    {
        public TrustedDeviceAuthorizationResultDto(string challenge) {
            Challenge = challenge ?? throw new ArgumentNullException(nameof(challenge));
        }

        [JsonPropertyName("challenge")]
        public string Challenge { get; }
    }
}
