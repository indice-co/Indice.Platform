using System;
using System.Collections.Generic;
using System.Text;
using Indice.AspNetCore.MultiTenancy;
using Indice.AspNetCore.MultiTenancy.Stores;
using Indice.AspNetCore.MultiTenancy.Strategies;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods over <see cref="TenantBuilder{T}"/>
    /// </summary>
    public static class TenantBuilderExtensions
    {
        /// <summary>
        /// Register the tenant store implementation In memory
        /// </summary>
        /// <param name="builder">The builder</param>
        /// <param name="tenants">Available tenants</param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public static TenantBuilder<T> WithInMemoryStore<T>(this TenantBuilder<T> builder, IEnumerable<T> tenants, ServiceLifetime lifetime = ServiceLifetime.Transient) where T : Tenant =>
            builder.WithStore((sp) => new InMemoryTenantStore<T>(tenants), lifetime);

        /// <summary>
        /// Will search to find the current tenant identifier from the currently running application Host. For example www.indice.gr
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public static TenantBuilder<T> FromHost<T>(this TenantBuilder<T> builder, ServiceLifetime lifetime = ServiceLifetime.Transient) where T : Tenant =>
            builder.WithResolutionStrategy<HostResolutionStrategy>(lifetime);

        /// <summary>
        /// Will search to find a route parameter in the current <see cref="Microsoft.AspNetCore.Routing.RouteData"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="routeParameterName">The route parameter name as defined in the attribute or convention based routing. ie: /subscriptions/{tenantId}/documents</param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        /// <remarks>In order to use this strategy ensure that the UseRouting call precedes the UseMultiTenancy middleware</remarks>
        public static TenantBuilder<T> FromRoute<T>(this TenantBuilder<T> builder, string routeParameterName = Constants.RouteParameterName, ServiceLifetime lifetime = ServiceLifetime.Transient) where T : Tenant =>
            builder.WithResolutionStrategy((sp) => {
                var accessor = sp.GetRequiredService<IHttpContextAccessor>();
                return new RouteResolutionStrategy(accessor, routeParameterName);
            }, lifetime);

        /// <summary>
        /// Will search to find a The http request header in the current http request.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="headerName">The http request header name from where to extract the tenant id. ie: X-Tenant-Id</param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        /// <remarks>In order to use this strategy ensure that the UseRouting call precedes the UseMultiTenancy middleware</remarks>
        public static TenantBuilder<T> FromHeader<T>(this TenantBuilder<T> builder, string headerName = Constants.HttpRequestHeaderName, ServiceLifetime lifetime = ServiceLifetime.Transient) where T : Tenant =>
            builder.WithResolutionStrategy((sp) => {
                var accessor = sp.GetRequiredService<IHttpContextAccessor>();
                return new HeaderResolutionStrategy(accessor, headerName);
            }, lifetime);
    }
}
