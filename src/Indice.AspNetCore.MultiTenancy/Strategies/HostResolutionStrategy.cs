using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.MultiTenancy.Strategies
{
    /* attribution: https://michael-mckenna.com/multi-tenant-asp-dot-net-core-application-tenant-resolution */
    /// <inheritdoc/>
    public class HostResolutionStrategy : ITenantResolutionStrategy
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Contructs the <see cref="HostResolutionStrategy"/> given the <see cref="IHttpContextAccessor"/>
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        public HostResolutionStrategy(IHttpContextAccessor httpContextAccessor) {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <inheritdoc/>
        public async Task<string> GetTenantIdentifierAsync() {
            return await Task.FromResult(_httpContextAccessor.HttpContext.Request.Host.Host);
        }
    }
}
