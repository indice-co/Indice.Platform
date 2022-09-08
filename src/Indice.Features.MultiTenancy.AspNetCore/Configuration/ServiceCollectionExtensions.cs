/* 
 * Attribution: https://michael-mckenna.com/multi-tenant-asp-dot-net-core-application-tenant-resolution 
 */

using Indice.Features.MultiTenancy.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Extensions related to multi-tenancy on type <see cref="IServiceCollection"/>.</summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>Adds the required services, using the specified tenant type.</summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <returns>The builder used to configure the multi-tenancy feature.</returns>
        public static TenantBuilder<TTenant> AddMultiTenancy<TTenant>(this IServiceCollection services) where TTenant : Tenant => new(services);

        /// <summary>Adds the required services, using the default tenant type.</summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <returns>The builder used to configure the multi-tenancy feature.</returns>
        public static TenantBuilder<Tenant> AddMultiTenancy(this IServiceCollection services) => new(services);
    }
}
