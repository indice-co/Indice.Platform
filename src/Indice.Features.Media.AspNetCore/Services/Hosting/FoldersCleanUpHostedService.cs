using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Media.AspNetCore.Services.Hosting;

/// <summary>Background service for cleaning up deleted folders from Db.</summary>
public class FoldersCleanUpHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FoldersCleanUpHostedService> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="FoldersCleanUpHostedService"/>
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="logger"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public FoldersCleanUpHostedService(IServiceProvider provider, ILogger<FoldersCleanUpHostedService> logger) {
        _serviceProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public TimeSpan Interval => TimeSpan.FromSeconds(60);

    /// <summary>
    /// Executes the background service's logic.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    /// <returns></returns>
    protected override Task ExecuteAsync(CancellationToken stoppingToken) {
        Task.Factory.StartNew(async () => {
            while (true) {
                if (stoppingToken.IsCancellationRequested) {
                    _logger.LogDebug("Cancellation was requested for {ServiceName}. Exiting.", nameof(FoldersCleanUpHostedService));
                    break;
                }
                try {
                    await Task.Delay(Interval, stoppingToken);
                    try {
                        using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
                            var mediaManager = serviceScope.ServiceProvider.GetRequiredService<MediaManager>();
                            await mediaManager.CleanUpFolders();
                        }
                    } catch (Exception exception) {
                        _logger.LogError("Exception while removing expired logs: {Exception}", exception.Message);
                    }
                } catch (TaskCanceledException exception) {
                    _logger.LogDebug("{ServiceName} was canceled. {Exception}", nameof(FoldersCleanUpHostedService), exception.Message);
                    break;
                } catch (Exception exception) {
                    _logger.LogDebug("An exception was thrown during {ServiceName} execution. {Exception}", nameof(FoldersCleanUpHostedService), exception.Message);
                    break;
                }
            }
        }, stoppingToken);
        return Task.CompletedTask;
    }
}
