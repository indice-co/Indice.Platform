using Indice.Features.Multitenancy.Core;
using Indice.Features.Multitenancy.Core.Stores;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods over <see cref="TenantBuilder{T}"/>.</summary>
public static class TenantBuilderExtensions
{
    /// <summary>Registers an in-memory implementation of the <see cref="ITenantStore{T}"/>.</summary>
    /// <typeparam name="TTenant">The type of the tenant.</typeparam>
    /// <param name="builder">The builder used to configure the multi-tenancy feature.</param>
    /// <param name="tenants">The collection of tenants to store in-memory.</param>
    /// <param name="lifetime">Specifies the lifetime of a service in an <see cref="IServiceCollection"/>.</param>
    /// <returns>The builder used to configure the multi-tenancy feature.</returns>
    public static TenantBuilder<TTenant> WithInMemoryStore<TTenant>(this TenantBuilder<TTenant> builder, IEnumerable<TTenant> tenants, ServiceLifetime lifetime = ServiceLifetime.Transient) where TTenant : Tenant =>
        builder.WithStore(serviceProvider => new TenantStoreInMemory<TTenant>(tenants), lifetime);
}
