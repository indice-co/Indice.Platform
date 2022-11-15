using System;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using Indice.AspNetCore.Identity.DeviceAuthentication.ResponseHandling;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Identity.DeviceAuthentication.Endpoints.Results
{
    internal class DeviceAuthenticationResult : IEndpointResult
    {
        public DeviceAuthenticationResult(DeviceAuthenticationResponse response) {
            Response = response ?? throw new ArgumentNullException(nameof(response));
        }

        public DeviceAuthenticationResponse Response { get; set; }

        public async Task ExecuteAsync(HttpContext context) {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.SetNoCache();
            var result = new DeviceAuthenticationResultDto(Response.Challenge);
            await context.Response.WriteJsonAsync(result);
        }
    }
}
