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

    public override async Task StopAsync(CancellationToken cancellationToken) {
        var pendingLogs = new List<ActivityLogEntry>();
        while (_ActivityLogEntryQueue.Reader.TryRead(out var logEntry)) {
            if (logEntry is not null) {
                pendingLogs.Add(logEntry);
            }
        }
        if (pendingLogs.Count > 0) {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
                var activityLogStore = serviceScope.ServiceProvider.GetRequiredService<IActivityLogStore>();
                var enricherAggregator = serviceScope.ServiceProvider.GetRequiredService<ActivityLogEntryEnricherAggregator>();
                var entriesToSave = new List<ActivityLogEntry>(pendingLogs.Count);
                try {
                    foreach (var logEntry in pendingLogs) {
                        var enrichResult = await enricherAggregator.EnrichAsync(
                            logEntry,
                            ActivityLogEnricherRunType.Default | ActivityLogEnricherRunType.Asynchronous);

                        if (enrichResult.Succeeded) {
                            await _eventService.Publish(new ActivityLogCreatedEvent(logEntry));
                            entriesToSave.Add(logEntry);
                        }
                    }
                    if (entriesToSave.Count > 0) {
                        await activityLogStore.CreateManyAsync(entriesToSave, cancellationToken);
                    }
                } 
                catch (Exception ex) {
                    _logger.LogCritical(ex, "CRITICAL: Database write failed during shutdown. {Count} in-memory audit logs were lost.", pendingLogs.Count);
                    throw;
                }
            }
        }
        await base.StopAsync(cancellationToken);
    }
}
