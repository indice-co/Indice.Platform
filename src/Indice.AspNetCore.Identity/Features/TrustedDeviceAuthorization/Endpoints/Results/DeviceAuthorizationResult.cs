using System;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.ResponseHandling;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Endpoints.Results
{
    internal class DeviceAuthorizationResult : IEndpointResult
    {
        public DeviceAuthorizationResult(DeviceAuthorizationResponse response) {
            Response = response ?? throw new ArgumentNullException(nameof(response));
        }

        public DeviceAuthorizationResponse Response { get; set; }

        public async Task ExecuteAsync(HttpContext context) {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.SetNoCache();
            var result = new TrustedDeviceAuthorizationResultDto(Response.Challenge);
            await context.Response.WriteJsonAsync(result);
        }
    }
}
