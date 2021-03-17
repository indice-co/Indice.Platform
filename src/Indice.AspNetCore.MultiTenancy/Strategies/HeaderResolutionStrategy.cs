using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.MultiTenancy.Strategies
{
    /// <inheritdoc/>
    public class HeaderResolutionStrategy : ITenantResolutionStrategy
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _headerName;


        /// <summary>
        /// Contructs the <see cref="HostResolutionStrategy"/> given the <see cref="IHttpContextAccessor"/>
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        /// <param name="headerName"></param>
        public HeaderResolutionStrategy(IHttpContextAccessor httpContextAccessor, string headerName) {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _headerName = headerName ?? throw new ArgumentNullException(nameof(headerName));
        }

        /// <inheritdoc/>
        public Task<string> GetTenantIdentifierAsync() {
            var values = (string)_httpContextAccessor.HttpContext.Request.Headers[_headerName];
            return Task.FromResult(string.IsNullOrEmpty(values) ? null : values);
        }
    }
}
