using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Indice.AspNetCore.MultiTenancy.Strategies
{
    /// <inheritdoc/>
    public class RouteResolutionStrategy : ITenantResolutionStrategy
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _routeParameterName;

        /// <summary>
        /// Contructs the <see cref="HostResolutionStrategy"/> given the <see cref="IHttpContextAccessor"/>
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        /// <param name="routeParameterName">The name of the route parameter</param>
        public RouteResolutionStrategy(IHttpContextAccessor httpContextAccessor, string routeParameterName) {
            _httpContextAccessor = httpContextAccessor;
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
