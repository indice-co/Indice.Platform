using Indice.AspNetCore.Features.Campaigns;
using Indice.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on <see cref="CampaignEndpointOptions"/>.
    /// </summary>
    public static class CampaignsApiOptionsExtensions
    {
        /// <summary>
        /// Adds <see cref="IEventDispatcher"/> using Azure Storage as a queuing mechanism.
        /// </summary>
        /// <param name="options">Options used to configure the Campaigns API feature.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static void UseEventDispatcherAzure(this CampaignEndpointOptions options, Action<IServiceProvider, EventDispatcherAzureOptions> configure = null) =>
            options.Services.AddEventDispatcherAzure(KeyedServiceNames.EventDispatcherAzureServiceKey, configure);
    }
}
