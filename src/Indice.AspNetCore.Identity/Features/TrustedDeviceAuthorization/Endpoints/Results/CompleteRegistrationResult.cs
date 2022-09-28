using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.ResponseHandling;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Endpoints.Results
{
    internal class CompleteRegistrationResult : IEndpointResult
    {
        public CompleteRegistrationResult(CompleteRegistrationResponse response) {
            Response = response ?? throw new ArgumentNullException(nameof(response));
        }

        public CompleteRegistrationResponse Response { get; }

        public async Task ExecuteAsync(HttpContext context) {
            context.Response.SetNoCache();
            if (Response.Errors.Any()) {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                var response = Response.Errors.ToValidationProblemDetails();
                await context.Response.WriteJsonAsync(response);
                return;
            }
            context.Response.StatusCode = StatusCodes.Status201Created;
            await context.Response.WriteJsonAsync(new TrustedDeviceCompleteRegistrationResultDto(Response.RegistrationId));
        }
    }
}
