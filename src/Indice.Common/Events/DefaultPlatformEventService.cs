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
        if (serviceScope.ServiceProvider.GetService(type) is not IEnumerable handlers) {
            return;
        }
        foreach (var handler in handlers) {
            var args = new PlatformEventArgs();
            try {
                var handleMethods = handler
                    .GetType()
                    .GetMethods()
                    .Where(method =>
                        method.Name == nameof(IPlatformEventHandler<IPlatformEvent>.Handle) &&
                        method.GetParameters()[0].ParameterType.Name.Equals(@event.GetType().Name, StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();
                foreach (var method in handleMethods) {
                    await (Task)method.Invoke(handler, [@event, args]);
                }
            } catch {
                if (args.ThrowOnError) {
                    throw;
                }
            }
        }
    }
}
