using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.ResponseHandling;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Endpoints.Results
{
    internal class InitRegistrationResult : IEndpointResult
    {
        public InitRegistrationResult(InitRegistrationResponse response) {
            Response = response ?? throw new ArgumentNullException(nameof(response));
        }

        public InitRegistrationResponse Response { get; }

        public async Task ExecuteAsync(HttpContext context) {
            context.Response.StatusCode = StatusCodes.Status200OK;
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
