using System;
using System.Collections.Generic;
using System.Text;
using Indice.AspNetCore.MultiTenancy;

namespace Microsoft.Extensions.DependencyInjection
{
    /* attribution: https://michael-mckenna.com/multi-tenant-asp-dot-net-core-application-tenant-resolution */
    /// <summary>
    /// Extensions related to MiltiTenancy on type <see cref="IServiceCollection"/>. 
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the services (application specific tenant class)
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static TenantBuilder<T> AddMultiTenancy<T>(this IServiceCollection services) where T : Tenant
            => new(services);

        /// <summary>
        /// Add the services (default tenant class)
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static TenantBuilder<Tenant> AddMultiTenancy(this IServiceCollection services)
            => new(services);
    }
}
