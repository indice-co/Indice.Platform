using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Identity.Features
{
    internal class CompleteRegistrationResult : IEndpointResult
    {
        public Task ExecuteAsync(HttpContext context) {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.SetNoCache();
            return Task.CompletedTask;
        }
    }
}
