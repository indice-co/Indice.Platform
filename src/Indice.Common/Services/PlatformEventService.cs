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
            var args = new PlatformEventArgs();
            try {
                await handler.Handle(@event, args);
            } catch (Exception) {
                if (args.ThrowOnError)
                    throw;
            }
            if (args.Handled)
                break;

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
