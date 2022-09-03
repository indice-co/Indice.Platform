using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.MultiTenancy.Strategies
{
    /// <inheritdoc/>
    public class HeaderResolutionStrategy : ITenantResolutionStrategy
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _headerName;

        /// <summary>Constructs a new instance of <see cref="HeaderResolutionStrategy"/> given the <see cref="IHttpContextAccessor"/>.</summary>
        /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>.</param>
        /// <param name="headerName">The name of the header to look for the tenant identifier.</param>
        public HeaderResolutionStrategy(IHttpContextAccessor httpContextAccessor, string headerName) {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _headerName = headerName ?? throw new ArgumentNullException(nameof(headerName));
        }

        /// <inheritdoc/>
        public Task<string> GetTenantIdentifierAsync() {
            var value = (string)_httpContextAccessor.HttpContext.Request.Headers[_headerName];
            return Task.FromResult(string.IsNullOrEmpty(value) ? null : value);
        }
    }
}
