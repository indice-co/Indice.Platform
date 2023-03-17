/* 
 * Attribution: https://michael-mckenna.com/multi-tenant-asp-dot-net-core-application-tenant-resolution 
 */

using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Multitenancy.Core;

/// <summary>A builder used to configure the multi-tenancy feature.</summary>
public class TenantBuilder<TTenant> where TTenant : Tenant
{
    private readonly IServiceCollection _services;

    /// <summary>Constructs the <see cref="TenantBuilder{T}"/> class.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public TenantBuilder(IServiceCollection services) {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>Registers the tenant resolver implementation.</summary>
    /// <typeparam name="TStrategy">The type of tenant resolution strategy.</typeparam>
    /// <param name="lifetime">Specifies the lifetime of a service in an <see cref="IServiceCollection"/>.</param>
    /// <returns>A builder used to configure the multi-tenancy feature.</returns>
    public TenantBuilder<TTenant> WithResolutionStrategy<TStrategy>(ServiceLifetime lifetime = ServiceLifetime.Transient) where TStrategy : class, ITenantResolutionStrategy {
        _services.Add(ServiceDescriptor.Describe(typeof(ITenantResolutionStrategy), typeof(TStrategy), lifetime));
        return this;
    }

    /// <summary>Registers the tenant resolver implementation.</summary>
    /// <typeparam name="TStrategy">The type of tenant resolution strategy.</typeparam>
    /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
    /// <param name="lifetime">Specifies the lifetime of a service in an <see cref="IServiceCollection"/>.</param>
    /// <returns>A builder used to configure the multi-tenancy feature.</returns>
    public TenantBuilder<TTenant> WithResolutionStrategy<TStrategy>(Func<IServiceProvider, TStrategy> implementationFactory, ServiceLifetime lifetime = ServiceLifetime.Transient) where TStrategy : class, ITenantResolutionStrategy {
        _services.Add(ServiceDescriptor.Describe(typeof(ITenantResolutionStrategy), implementationFactory, lifetime));
        return this;
    }

    /// <summary>Registers a tenant store implementation.</summary>
    /// <typeparam name="TStore">The type of tenant store.</typeparam>
    /// <param name="lifetime">Specifies the lifetime of a service in an <see cref="IServiceCollection"/>.</param>
    /// <returns>A builder used to configure the multi-tenancy feature.</returns>
    public TenantBuilder<TTenant> WithStore<TStore>(ServiceLifetime lifetime = ServiceLifetime.Transient) where TStore : class, ITenantStore<TTenant> {
        _services.Add(ServiceDescriptor.Describe(typeof(ITenantStore<TTenant>), typeof(TStore), lifetime));
        return this;
    }

    /// <summary>Registers a tenant store implementation.</summary>
    /// <typeparam name="TStore">The type of tenant store.</typeparam>
    /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
    /// <param name="lifetime">Specifies the lifetime of a service in an <see cref="IServiceCollection"/>.</param>
    /// <returns>A builder used to configure the multi-tenancy feature.</returns>
    public TenantBuilder<TTenant> WithStore<TStore>(Func<IServiceProvider, TStore> implementationFactory, ServiceLifetime lifetime = ServiceLifetime.Transient) where TStore : class, ITenantStore<TTenant> {
        _services.Add(ServiceDescriptor.Describe(typeof(ITenantStore<TTenant>), implementationFactory, lifetime));
        return this;
    }
}
