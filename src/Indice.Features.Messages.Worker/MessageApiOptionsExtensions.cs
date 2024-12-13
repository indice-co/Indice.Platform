using Indice.Features.Messages.Core;
using Indice.Hosting.Services;
using Indice.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions on <see cref="MessageEndpointOptions"/>.</summary>
public static class MessageApiOptionsExtensions
{
    /// <summary>Adds <see cref="IEventDispatcher"/> using Indice worker host as a queuing mechanism.</summary>
    /// <param name="options">Options used to configure the Campaigns API feature.</param>
    public static void UseEventDispatcherHosting(this MessageEndpointOptions options) {
        options.Services!.TryAddTransient<IEventDispatcherFactory, DefaultEventDispatcherFactory>();
        options.Services!.AddKeyedTransient<IEventDispatcher, EventDispatcherHosting>(
            serviceKey: KeyedServiceNames.EventDispatcherServiceKey,
            (serviceProvider, key) => new EventDispatcherHosting(new MessageQueueFactory(serviceProvider))
        );
    }
}
