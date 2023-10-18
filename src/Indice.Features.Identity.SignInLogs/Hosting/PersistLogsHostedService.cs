using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Enrichers;
using Indice.Features.Identity.SignInLogs.Events;
using Indice.Features.Identity.SignInLogs.Models;
using Indice.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.SignInLogs.Hosting;

internal class PersistLogsHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SignInLogEntryQueue _signInLogEntryQueue;
    private readonly IPlatformEventService _eventService;
    private readonly ILogger<PersistLogsHostedService> _logger;

    public PersistLogsHostedService(
        IServiceProvider serviceProvider,
        SignInLogEntryQueue signInLogEntryQueue,
        IPlatformEventService eventService,
        ILogger<PersistLogsHostedService> logger
    ) {
        _signInLogEntryQueue = signInLogEntryQueue ?? throw new ArgumentNullException(nameof(signInLogEntryQueue));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
            var signInLogStore = serviceScope.ServiceProvider.GetRequiredService<ISignInLogStore>();
            var enricherAggregator = serviceScope.ServiceProvider.GetRequiredService<SignInLogEntryEnricherAggregator>();
            // possible optimization read in batch so that we have fewer roundtrips to database https://stackoverflow.com/questions/63881607/how-to-read-remaining-items-in-channelt-less-than-batch-size-if-there-is-no-n
            // https://github.com/Open-NET-Libraries/Open.ChannelExtensions#batching
            await foreach (var logEntry in _signInLogEntryQueue.DequeueAllAsync().WithCancellation(stoppingToken)) {
                var enrichResult = await enricherAggregator.EnrichAsync(logEntry, SignInLogEnricherRunType.Default | SignInLogEnricherRunType.Asynchronous);
                if (enrichResult.Succeeded) {
                    await signInLogStore.CreateAsync(logEntry, stoppingToken);
                    await _eventService.Publish(new SignInLogCreatedEvent(logEntry));
                }
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("{ServiceName} is shutting down", nameof(PersistLogsHostedService));
        // TODO: Consider persisting remaining sign in log entries on application shutdown.
        return base.StopAsync(cancellationToken);
    }
}
