using Indice.Features.Messages.Core;
using Indice.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Messages.Worker.Azure
{
    /// <summary>
    /// Extensions on <see cref="MessageEndpointOptions"/>.
    /// </summary>
    public static class MessageApiOptionsExtensions
    {
        /// <summary>
        /// Adds <see cref="IEventDispatcher"/> using Azure Storage as a queuing mechanism.
        /// </summary>
        /// <param name="options">Options used to configure the Campaigns API feature.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static void UseEventDispatcherAzure(this MessageEndpointOptions options, Action<IServiceProvider, EventDispatcherAzureOptions> configure = null) =>
            options.Services.AddEventDispatcherAzure(KeyedServiceNames.EventDispatcherServiceKey, configure);
    }
}
