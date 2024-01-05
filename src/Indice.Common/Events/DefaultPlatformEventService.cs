using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Indice.Events;

/// <summary>Implementation of <see cref="IPlatformEventService"/> that runs platform events synchronously as part of the request lifecycle.</summary>
public class DefaultPlatformEventService : IPlatformEventService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    /// <summary>Creates a new instance of <see cref="DefaultPlatformEventService"/>.</summary>
    /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
    /// <param name="logger">A parameter will allow for a logger to be created</param>
    public DefaultPlatformEventService(IServiceProvider serviceProvider, ILogger<DefaultPlatformEventService> logger) {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task Publish(IPlatformEvent @event) {
        using var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var type = typeof(IPlatformEventHandler<>).MakeGenericType(@event.GetType());
        var handleMethod = type.GetMethod(nameof(IPlatformEventHandler<IPlatformEvent>.Handle));
        var handlers = serviceScope.ServiceProvider.GetService(typeof(IEnumerable<>).MakeGenericType(type)) as IEnumerable;
        handlers ??= Enumerable.Empty<IPlatformEventHandler<IPlatformEvent>>();

        foreach (var handler in handlers) {
            var args = new PlatformEventArgs();
            try {
                await (Task)handleMethod.Invoke(handler, [@event, args]);
            } catch (Exception ex) {
                if (args.ThrowOnError) {
                    throw;
                }
                _logger.LogError(ex, "Failed to invoke handler type: {PlatformEventHandler}", handler.GetType().Name);
            }
        }
    }
}
