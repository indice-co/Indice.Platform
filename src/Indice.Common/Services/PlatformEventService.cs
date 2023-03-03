using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Services;

/// <summary>Implementation of <see cref="IPlatformEventService"/>.</summary>
public class PlatformEventService : IPlatformEventService
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Creates a new instance of <see cref="PlatformEventService"/>.</summary>
    /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
    public PlatformEventService(IServiceProvider serviceProvider) {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public async Task Publish<TEvent>(TEvent @event) where TEvent : IPlatformEvent {
        var handlers = _serviceProvider.GetService<IEnumerable<IPlatformEventHandler<TEvent>>>() ?? Enumerable.Empty<IPlatformEventHandler<TEvent>>();
        foreach (var handler in handlers) {
            await handler.Handle(@event);
        }
    }
}

/// <summary>Models the event handler.</summary>
/// <typeparam name="TEvent">The type of the event raised.</typeparam>
public interface IPlatformEventHandler<TEvent> where TEvent : IPlatformEvent
{
    /// <summary>The method used to handle the event creation.</summary>
    /// <param name="event">The type of the event raised.</param>
    /// <returns>The <see cref="Task"/> that was successfully completed.</returns>
    Task Handle(TEvent @event);
}
