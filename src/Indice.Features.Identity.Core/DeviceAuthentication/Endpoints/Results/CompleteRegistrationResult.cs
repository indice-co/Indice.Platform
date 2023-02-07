using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using Indice.Features.Identity.Core.DeviceAuthentication.ResponseHandling;
using Indice.Features.Identity.Core.Extensions;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Endpoints.Results
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
            await context.Response.WriteJsonAsync(new CompleteRegistrationResultDto(Response.RegistrationId));
        }
    }
}
