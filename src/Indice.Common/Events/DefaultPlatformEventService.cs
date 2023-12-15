using System.Collections;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Events;

/// <summary>Implementation of <see cref="IPlatformEventService"/> that runs platform events synchronously as part of the request lifecycle.</summary>
public class DefaultPlatformEventService : IPlatformEventService
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Creates a new instance of <see cref="DefaultPlatformEventService"/>.</summary>
    /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
    public DefaultPlatformEventService(IServiceProvider serviceProvider) {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public async Task Publish(IPlatformEvent @event) {
        using var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var type = typeof(IEnumerable<>).MakeGenericType(typeof(IPlatformEventHandler<>).MakeGenericType(@event.GetType()));
        var handlers = serviceScope.ServiceProvider.GetService(type) as IEnumerable;
        handlers ??= Enumerable.Empty<IPlatformEventHandler<IPlatformEvent>>();
        foreach (var handler in handlers) {
            var args = new PlatformEventArgs();
            try {
                var handleMethod = handler.GetType().GetMethod(nameof(IPlatformEventHandler<IPlatformEvent>.Handle));
                if (handleMethod is not null) {
                    await (Task)handleMethod.Invoke(handler, new object[] { @event, args });
                }
            } catch {
                if (args.ThrowOnError) {
                    throw;
                }
            }
        }
    }
}
