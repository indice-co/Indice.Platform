using Microsoft.Extensions.DependencyInjection;

namespace Indice.Events;

/// <summary>Implementation of <see cref="IPlatformEventService"/> that runs platform events synchronously as part of the request context.</summary>
public class DefaultPlatformEventService : IPlatformEventService
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Creates a new instance of <see cref="DefaultPlatformEventService"/>.</summary>
    /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
    public DefaultPlatformEventService(IServiceProvider serviceProvider) {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

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
