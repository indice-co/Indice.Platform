using Indice.Features.Multitenancy.Core;
using Indice.Features.Multitenancy.Worker.Azure;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions related to multi-tenancy on type <see cref="IServiceCollection"/>.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Adds the required services, using the specified tenant type.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <returns>The builder used to configure the multi-tenancy feature.</returns>
    public static TenantBuilder<TTenant> AddMultiTenancy<TTenant>(this IServiceCollection services) where TTenant : Tenant {
        var builder = services.AddMultiTenancyCore<TTenant>();
        services.AddFunctionContextAccessor();
        services.AddTransient<ITenantAccessor<TTenant>, TenantAccessorFunctionContext<TTenant>>();
        return builder;
    }

    /// <summary>Adds the required services, using the default tenant type.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <returns>The builder used to configure the multi-tenancy feature.</returns>
    public static TenantBuilder<Tenant> AddMultiTenancy(this IServiceCollection services) => services.AddMultiTenancy<Tenant>();
}
