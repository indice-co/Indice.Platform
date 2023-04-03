using Indice.AspNetCore.Hosting;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Enrichers;
using Indice.Features.Identity.SignInLogs.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.SignInLogs.Hosting;

internal class PersistLogsHostedService : TimedHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SignInLogEntryQueue _signInLogEntryQueue;

    public PersistLogsHostedService(
        ILogger<LogCleanupHostedService> logger,
        IServiceProvider serviceProvider,
        SignInLogEntryQueue signInLogEntryQueue
    ) : base(logger) {
        _signInLogEntryQueue = signInLogEntryQueue ?? throw new ArgumentNullException(nameof(signInLogEntryQueue));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public override TimeSpan Interval => TimeSpan.FromMilliseconds(300);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        try {
            using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
                var signInLogStore = serviceScope.ServiceProvider.GetRequiredService<ISignInLogStore>();
                var enricherAggregator = serviceScope.ServiceProvider.GetRequiredService<SignInLogEntryEnricherAggregator>();
                await foreach (var logEntry in _signInLogEntryQueue.DequeueAllAsync()) {
                    var discard = await enricherAggregator.EnrichAsync(logEntry, EnricherDependencyType.Default | EnricherDependencyType.OnDataStore);
                    if (!discard) {
                        await signInLogStore.CreateAsync(logEntry);
                    }
                }
            }
        } catch (Exception exception) {
            Logger.LogError("Exception while removing expired logs: {Exception}", exception.Message);
        }
    }
}
