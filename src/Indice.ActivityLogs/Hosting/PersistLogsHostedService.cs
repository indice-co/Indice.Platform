using Indice.Events;
using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Enrichers;
using Indice.Features.ActivityLogs.Events;
using Indice.Features.ActivityLogs.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.ChannelExtensions;

namespace Indice.Features.ActivityLogs.Hosting;

internal class PersistLogsHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ActivityLogEntryQueue _ActivityLogEntryQueue;
    private readonly IPlatformEventService _eventService;
    private readonly ILogger<PersistLogsHostedService> _logger;
    private readonly ActivityLogOptions _ActivityLogOptions;

    public PersistLogsHostedService(
        IServiceProvider serviceProvider,
        ActivityLogEntryQueue ActivityLogEntryQueue,
        IPlatformEventService eventService,
        ILogger<PersistLogsHostedService> logger,
        IOptions<ActivityLogOptions> ActivityLogOptions
    ) {
        _ActivityLogEntryQueue = ActivityLogEntryQueue ?? throw new ArgumentNullException(nameof(ActivityLogEntryQueue));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _ActivityLogOptions = ActivityLogOptions?.Value ?? throw new ArgumentNullException(nameof(ActivityLogOptions));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
            var ActivityLogStore = serviceScope.ServiceProvider.GetRequiredService<IActivityLogStore>();
            var enricherAggregator = serviceScope.ServiceProvider.GetRequiredService<ActivityLogEntryEnricherAggregator>();
            // Possible optimization read in batch so that we have fewer roundtrips to database https://stackoverflow.com/questions/63881607/how-to-read-remaining-items-in-channelt-less-than-batch-size-if-there-is-no-n
            // https://github.com/Open-NET-Libraries/Open.ChannelExtensions#batching
            var events = _ActivityLogEntryQueue.Reader
                .PipeAsync(async logEntry => {
                    var enrichResult = await enricherAggregator.EnrichAsync(logEntry, ActivityLogEnricherRunType.Default | ActivityLogEnricherRunType.Asynchronous);
                    if (enrichResult.Succeeded) {
                        return logEntry;
                    }
                    return null;
                }, cancellationToken: stoppingToken)
                .Filter(logEntry => logEntry is not null)
                .PipeAsync(async logEntry => {
                    await _eventService.Publish(new ActivityLogCreatedEvent(logEntry!));
                    return logEntry;
                }, cancellationToken: stoppingToken)
                .Batch(_ActivityLogOptions.DequeueBatchSize)
                .WithTimeout(_ActivityLogOptions.DequeueTimeoutInMilliseconds)
                .ReadAllAsync(stoppingToken);
            await foreach (var logEntryBatch in events) {
                await ActivityLogStore.CreateManyAsync(logEntryBatch!, stoppingToken);
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("{ServiceName} is shutting down", nameof(PersistLogsHostedService));
        // TODO: Consider persisting remaining activity log entries on application shutdown.
        return base.StopAsync(cancellationToken);
    }
}
