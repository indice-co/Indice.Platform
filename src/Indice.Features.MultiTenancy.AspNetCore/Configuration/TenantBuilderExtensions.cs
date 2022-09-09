using Indice.Features.Multitenancy.AspNetCore;
using Indice.Features.Multitenancy.AspNetCore.Strategies;
using Indice.Features.Multitenancy.Core;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Extension methods over <see cref="TenantBuilder{T}"/>.</summary>
    public static class TenantBuilderExtensions
    {
        /// <summary>Will search to find the current tenant identifier from the currently running application host. For example www.indice.gr</summary>
        /// <typeparam name="TTenant">The type of the tenant.</typeparam>
        /// <param name="builder">The builder used to configure the multi-tenancy feature.</param>
        /// <param name="lifetime">Specifies the lifetime of a service in an <see cref="IServiceCollection"/>.</param>
        /// <returns>The builder used to configure the multi-tenancy feature.</returns>
        public static TenantBuilder<TTenant> FromHost<TTenant>(this TenantBuilder<TTenant> builder, ServiceLifetime lifetime = ServiceLifetime.Transient) where TTenant : Tenant =>
            builder.WithResolutionStrategy<HostResolutionStrategy>(lifetime);

        /// <summary>Will search to find a route parameter in the current <see cref="AspNetCore.Routing.RouteData"/>.</summary>
        /// <typeparam name="TTenant">The type of the tenant.</typeparam>
        /// <param name="builder">The builder used to configure the multi-tenancy feature.</param>
        /// <param name="routeParameterName">The route parameter name as defined in the attribute or convention based routing. <b>i.e: /subscriptions/{tenantId}/documents</b></param>
        /// <param name="lifetime">Specifies the lifetime of a service in an <see cref="IServiceCollection"/>.</param>
        /// <returns>The builder used to configure the multi-tenancy feature.</returns>
        /// <remarks>In order to use this strategy, ensure that the UseRouting call precedes the UseMultiTenancy middleware.</remarks>
        public static TenantBuilder<TTenant> FromRoute<TTenant>(this TenantBuilder<TTenant> builder, string routeParameterName = Constants.RouteParameterName, ServiceLifetime lifetime = ServiceLifetime.Transient) where TTenant : Tenant =>
            builder.WithResolutionStrategy(serviceProvider => {
                var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                return new RouteResolutionStrategy(httpContextAccessor, routeParameterName);
            }, lifetime);

        /// <summary>Will search to find a HTTP request header in the current request.</summary>
        /// <typeparam name="TTenant">The type of the tenant.</typeparam>
        /// <param name="builder">The builder used to configure the multi-tenancy feature.</param>
        /// <param name="headerName">The HTTP request header name from where to extract the tenant id. <b>i.e: X-Tenant-Id</b></param>
        /// <param name="lifetime">The builder used to configure the multi-tenancy feature.</param>
        /// <returns></returns>
        /// <remarks>In order to use this strategy ensure that the UseRouting call precedes the UseMultiTenancy middleware</remarks>
        public static TenantBuilder<TTenant> FromHeader<TTenant>(this TenantBuilder<TTenant> builder, string headerName = Constants.HttpRequestHeaderName, ServiceLifetime lifetime = ServiceLifetime.Transient) where TTenant : Tenant =>
            builder.WithResolutionStrategy(serviceProvider => {
                var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                return new HeaderResolutionStrategy(httpContextAccessor, headerName);
            }, lifetime);
    }
}
