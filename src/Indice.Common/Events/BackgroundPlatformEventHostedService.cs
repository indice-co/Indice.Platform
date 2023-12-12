#if !NETSTANDARD2_1
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Events;

internal class BackgroundPlatformEventHostedService<TEvent> : BackgroundService where TEvent : IPlatformEvent
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BackgroundPlatformEventServiceQueue<TEvent> _queue;
    private readonly IPlatformEventService _eventService;
    private readonly ILogger<BackgroundPlatformEventHostedService<TEvent>> _logger;
    private readonly BackgroundPlatformEventServiceQueueOptions _options;

    public BackgroundPlatformEventHostedService(
        IServiceProvider serviceProvider,
        BackgroundPlatformEventServiceQueue<TEvent> queue,
        IPlatformEventService eventService,
        ILogger<BackgroundPlatformEventHostedService<TEvent>> logger,
        IOptions<BackgroundPlatformEventServiceQueueOptions> options
    ) {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
            // Possible optimization read in batch so that we have fewer roundtrips to database https://stackoverflow.com/questions/63881607/how-to-read-remaining-items-in-channelt-less-than-batch-size-if-there-is-no-n
            // https://github.com/Open-NET-Libraries/Open.ChannelExtensions#batching
            var events = _queue.Reader.ReadAllAsync(stoppingToken);
            await foreach (var @event in events) {
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

    public override Task StopAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("{ServiceName} is shutting down", nameof(BackgroundPlatformEventHostedService<TEvent>));
        // TODO: Consider persisting remaining sign in log entries on application shutdown.
        return base.StopAsync(cancellationToken);
    }
}
#endif
