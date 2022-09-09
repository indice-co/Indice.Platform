using Indice.Features.Multitenancy.Worker.Azure.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Multitenancy.Core
{
    /// <summary>Extension methods over <see cref="TenantBuilder{T}"/>.</summary>
    public static class TenantBuilderExtensions
    {
        /// <summary>Will search to find the current tenant identifier from the currently running application host. For example www.indice.gr</summary>
        /// <typeparam name="TTenant">The type of the tenant.</typeparam>
        /// <param name="builder">The builder used to configure the multi-tenancy feature.</param>
        /// <param name="lifetime">Specifies the lifetime of a service in an <see cref="IServiceCollection"/>.</param>
        /// <returns>The builder used to configure the multi-tenancy feature.</returns>
        public static TenantBuilder<TTenant> FromQueueTriggerPayload<TTenant>(this TenantBuilder<TTenant> builder, ServiceLifetime lifetime = ServiceLifetime.Transient) where TTenant : Tenant =>
            builder.WithResolutionStrategy<FromQueueTriggerPayloadResolutionStrategy>(lifetime);
    }
}
