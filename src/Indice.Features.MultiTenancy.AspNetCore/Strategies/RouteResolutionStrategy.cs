using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Indice.Features.Multitenancy.AspNetCore.Strategies
{
    /// <inheritdoc/>
    public class RouteResolutionStrategy : ITenantResolutionStrategy
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _routeParameterName;

        /// <summary>Constructs the <see cref="RouteResolutionStrategy"/> given the <see cref="IHttpContextAccessor"/>.</summary>
        /// <param name="httpContextAccessor">Provides access to the current HTTP context.</param>
        /// <param name="routeParameterName">The name of the route parameter.</param>
        public RouteResolutionStrategy(IHttpContextAccessor httpContextAccessor, string routeParameterName) {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _routeParameterName = routeParameterName ?? throw new ArgumentNullException(nameof(routeParameterName));
        }

        /// <inheritdoc/>
        public Task<string> GetTenantIdentifierAsync() {
            var route = _httpContextAccessor.HttpContext.GetRouteData();
            route.Values.TryGetValue(_routeParameterName, out var identifier);
            return Task.FromResult(identifier?.ToString());
        }
    }
}
