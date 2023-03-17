using Indice.Features.Multitenancy.Core;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions related to multi-tenancy on type <see cref="IServiceCollection"/>.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Adds the required services, using the specified tenant type.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <returns>The builder used to configure the multi-tenancy feature.</returns>
    public static TenantBuilder<TTenant> AddMultiTenancyCore<TTenant>(this IServiceCollection services) where TTenant : Tenant {
        var builder = new TenantBuilder<TTenant>(services);
        if (typeof(TTenant).Equals(typeof(Tenant))) {
            services.AddTransient<DefaultTenantAccessService>();
        }
        services.AddTransient<TenantAccessService<TTenant>>();
        return builder;
    }

    /// <summary>Adds the required services, using the default tenant type.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <returns>The builder used to configure the multi-tenancy feature.</returns>
    public static TenantBuilder<Tenant> AddMultiTenancyCore(this IServiceCollection services) => services.AddMultiTenancyCore<Tenant>();
}
