using System;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.ResponseHandling;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Identity.DeviceAuthentication.Endpoints.Results
{
    internal class DeviceAuthenticationErrorResult : IEndpointResult
    {
        private readonly TokenErrorResponse _tokenErrorResponse;

        public DeviceAuthenticationErrorResult(TokenErrorResponse tokenErrorResponse) {
            if (string.IsNullOrWhiteSpace(tokenErrorResponse.Error)) {
                throw new ArgumentNullException(nameof(tokenErrorResponse.Error), "Error must be set.");
            }
            _tokenErrorResponse = tokenErrorResponse;
        }

        public async Task ExecuteAsync(HttpContext context) {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.SetNoCache();
            var result = new ErrorResultDto {
                Error = _tokenErrorResponse.Error,
                ErrorDescription = _tokenErrorResponse.ErrorDescription,
                Custom = _tokenErrorResponse.Custom
            };
            await context.Response.WriteJsonAsync(result);
        }
    }
}
