using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Events;

internal class BackgroundPlatformEventHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BackgroundPlatformEventServiceQueue _queue;
    private readonly IPlatformEventService _eventService;
    private readonly ILogger<BackgroundPlatformEventHostedService> _logger;
    private readonly BackgroundPlatformEventServiceQueueOptions _options;

    public BackgroundPlatformEventHostedService(
        IServiceProvider serviceProvider,
        BackgroundPlatformEventServiceQueue queue,
        IPlatformEventService eventService,
        ILogger<BackgroundPlatformEventHostedService> logger,
        IOptions<BackgroundPlatformEventServiceQueueOptions> options
    ) {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        using var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var events = _queue.DequeueAsync(stoppingToken);
        await foreach (var @event in events) {
            // Resolve Handle through the event specific interface, since a handler may implement it for more than one event type.
            var handlerType = typeof(IPlatformEventHandler<>).MakeGenericType(@event.GetType());
            var handleMethod = handlerType.GetMethod(nameof(IPlatformEventHandler<IPlatformEvent>.Handle))!;
            var handlers = serviceScope.ServiceProvider.GetService(typeof(IEnumerable<>).MakeGenericType(handlerType)) as IEnumerable;
            handlers ??= Enumerable.Empty<IPlatformEventHandler<IPlatformEvent>>();
            foreach (var handler in handlers) {
                var args = new PlatformEventArgs();
                while (true) {
                    try {
                        await (Task)handleMethod.Invoke(handler, [ @event, args ])!;
                        break;
                    } catch (Exception ex) {
                        if (!args.ThrowOnError) {
                            _logger.LogError(ex, "Failed to invoke handler type: {PlatformEventHandler}", handler.GetType().Name);
                            break;
                        }
                        _logger.LogError(ex, "Failed to invoke required handler type: {PlatformEventHandler}; retrying.", handler.GetType().Name);
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    }
                }
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("{ServiceName} is shutting down", nameof(BackgroundPlatformEventHostedService));
        // TODO: Consider persisting remaining sign in log entries on application shutdown.
        return base.StopAsync(cancellationToken);
    }
}