/* 
 * Attribution: https://michael-mckenna.com/multi-tenant-asp-dot-net-core-application-tenant-resolution 
 */

using Indice.AspNetCore.MultiTenancy;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>Extension methods on <see cref="IApplicationBuilder"/> to register multi-tenancy middleware.</summary>
    public static class IApplicationBuilderExtensions
    {
        /// <summary>Registers multi-tenancy middleware.</summary>
        /// <typeparam name="TTenant">The type of the tenant.</typeparam>
        /// <param name="builder">The builder used to configure the multi-tenancy feature.</param>
        public static IApplicationBuilder UseMultiTenancy<TTenant>(this IApplicationBuilder builder) where TTenant : Tenant => builder.UseMiddleware<TenantMiddleware<TTenant>>();

        /// <summary>Registers multi-tenancy middleware.</summary>
        /// <param name="builder">The builder used to configure the multi-tenancy feature.</param>
        public static IApplicationBuilder UseMultiTenancy(this IApplicationBuilder builder) => builder.UseMiddleware<TenantMiddleware<Tenant>>();
    }
}
