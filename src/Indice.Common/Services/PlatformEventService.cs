using Microsoft.Extensions.DependencyInjection;

namespace Indice.Services;

/// <summary>Implementation of <see cref="IPlatformEventService"/>.</summary>
public class PlatformEventService : IPlatformEventService
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Creates a new instance of <see cref="PlatformEventService"/>.</summary>
    /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
    public PlatformEventService(IServiceProvider serviceProvider) =>
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <inheritdoc />
    public async Task Publish<TEvent>(TEvent @event) where TEvent : IPlatformEvent {
        using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
            var handlers = serviceScope.ServiceProvider.GetService<IEnumerable<IPlatformEventHandler<TEvent>>>() ?? Enumerable.Empty<IPlatformEventHandler<TEvent>>();
            foreach (var handler in handlers) {
                var args = new PlatformEventArgs();
                try {
                    await handler.Handle(@event, args);
                } catch {
                    if (args.ThrowOnError) {
                        throw;
                    }
                }
            }
        }
    }
}

/// <summary>Models the event handler.</summary>
/// <typeparam name="TEvent">The type of the event raised.</typeparam>
public interface IPlatformEventHandler<TEvent> where TEvent : IPlatformEvent
{
    /// <summary>The method used to handle the event creation.</summary>
    /// <param name="event">The type of the event raised.</param>
    /// <param name="args">Arguments to communicate back to the event publisher</param>
    /// <returns>The <see cref="Task"/> that was successfully completed.</returns>
    Task Handle(TEvent @event, PlatformEventArgs args);
}

/// <summary>Extension methods on <see cref="IPlatformEventHandler{TEvent}"/>.</summary>
public static class IPlatformEventHandlerExtensions
{
    /// <summary>The method used to handle the event creation using the default setup for <see cref="PlatformEventArgs"/>.</summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="eventHandler">Models the event handler.</param>
    /// <param name="event">The type of the event raised.</param>
    public static Task Handle<TEvent>(this IPlatformEventHandler<TEvent> eventHandler, TEvent @event) where TEvent : IPlatformEvent =>
        eventHandler.Handle(@event, new PlatformEventArgs { ThrowOnError = false });
}
