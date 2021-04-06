using System;
using System.Threading.Tasks;
using IdentityServer4.Hosting;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Identity.Features
{
    internal class TrustedDeviceCompleteRegistrationEndpoint : IEndpointHandler
    {
        public Task<IEndpointResult> ProcessAsync(HttpContext context) {
            throw new NotImplementedException();
        }
    }
}
