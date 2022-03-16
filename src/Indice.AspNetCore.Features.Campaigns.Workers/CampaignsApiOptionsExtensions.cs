using Indice.AspNetCore.Features.Campaigns;
using Indice.Hosting.Services;
using Indice.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on <see cref="CampaignsApiOptions"/>.
    /// </summary>
    public static class CampaignsApiOptionsExtensions
    {
        /// <summary>
        /// Adds <see cref="IEventDispatcher"/> using Azure Storage as a queuing mechanism.
        /// </summary>
        /// <param name="options">Options used to configure the Campaigns API feature.</param>
        public static void UseEventDispatcherHosting(this CampaignsApiOptions options) =>
            options.Services.AddKeyedService<IEventDispatcher, EventDispatcherHosting, string>(
                key: KeyedServiceNames.EventDispatcherAzureServiceKey,
                serviceProvider => new EventDispatcherHosting(new MessageQueueFactory(serviceProvider)),
                serviceLifetime: ServiceLifetime.Transient
            );
    }
}
