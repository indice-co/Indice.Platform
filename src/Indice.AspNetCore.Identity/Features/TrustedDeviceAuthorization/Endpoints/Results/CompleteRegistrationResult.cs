using System;
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

        public Task ExecuteAsync(HttpContext context) {
            context.Response.StatusCode = StatusCodes.Status201Created;
            context.Response.SetNoCache();
            return Task.CompletedTask;
        }
    }
}
