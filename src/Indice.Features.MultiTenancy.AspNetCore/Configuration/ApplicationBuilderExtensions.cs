/* 
 * Attribution: https://michael-mckenna.com/multi-tenant-asp-dot-net-core-application-tenant-resolution 
 */

using Indice.Features.Multitenancy.AspNetCore;
using Indice.Features.Multitenancy.Core;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Extension methods on <see cref="IApplicationBuilder"/> to register multi-tenancy middleware.</summary>
public static class IApplicationBuilderExtensions
{
    /// <summary>Registers multi-tenancy middleware.</summary>
    /// <typeparam name="TTenant">The type of the tenant.</typeparam>
    /// <param name="builder">The builder used to configure the multi-tenancy feature.</param>
    [Obsolete("AddMultiTenancy already adds the TenantMiddleware so this is not required any more")]
    public static IApplicationBuilder UseMultiTenancy<TTenant>(this IApplicationBuilder builder) where TTenant : Tenant => builder;

    /// <summary>Registers multi-tenancy middleware.</summary>
    /// <param name="builder">The builder used to configure the multi-tenancy feature.</param>
    [Obsolete("AddMultiTenancy already adds the TenantMiddleware so this is not required any more")]
    public static IApplicationBuilder UseMultiTenancy(this IApplicationBuilder builder) => builder;
}
