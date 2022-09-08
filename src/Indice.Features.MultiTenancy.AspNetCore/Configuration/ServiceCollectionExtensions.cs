/* 
 * Attribution: https://michael-mckenna.com/multi-tenant-asp-dot-net-core-application-tenant-resolution 
 */

using Indice.Features.Multitenancy.AspNetCore;
using Indice.Features.Multitenancy.AspNetCore.Authorization;
using Indice.Features.Multitenancy.Core;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Extensions related to multi-tenancy on type <see cref="IServiceCollection"/>.</summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>Adds the required services, using the specified tenant type.</summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <returns>The builder used to configure the multi-tenancy feature.</returns>
        public static TenantBuilder<TTenant> AddMultiTenancy<TTenant>(this IServiceCollection services) where TTenant : Tenant {
            var builder = services.AddMultiTenancyCore<TTenant>();
            services.AddHttpContextAccessor();
            services.AddDistributedMemoryCache();
            services.AddTransient<ITenantAccessor<TTenant>, TenantAccessorHttpContext<TTenant>>();
            services.AddTransient<IAuthorizationHandler, BeTenantMemberHandler<TTenant>>();
            return builder;
        }

        /// <summary>Adds the required services, using the default tenant type.</summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <returns>The builder used to configure the multi-tenancy feature.</returns>
        public static TenantBuilder<Tenant> AddMultiTenancy(this IServiceCollection services) => services.AddMultiTenancy<Tenant>();
    }
}
