using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.ActivityLogs.Hosting;

internal class LogCleanupHostedService : BackgroundService
{
    private readonly ILogger<LogCleanupHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ActivityLogOptions _ActivityLogOptions;

    public LogCleanupHostedService(
        ILogger<LogCleanupHostedService> logger,
        IServiceProvider serviceProvider,
        IOptions<ActivityLogOptions> ActivityLogOptions
    ) {
        _ActivityLogOptions = ActivityLogOptions?.Value ?? throw new ArgumentNullException(nameof(ActivityLogOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }
    public TimeSpan Interval => TimeSpan.FromSeconds(_ActivityLogOptions.Cleanup.IntervalSeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            try {
                await Task.Delay(Interval, stoppingToken);
                using var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
                var activityLogStore = serviceScope.ServiceProvider.GetRequiredService<IActivityLogStore>();
                await activityLogStore.Cleanup(stoppingToken);
            } 
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                _logger.LogDebug("{ServiceName} is stopping.", nameof(LogCleanupHostedService));
                break;
            } 
            catch (Exception exception) {
                _logger.LogError(exception, "Exception while removing expired logs.");
            }
        }
    }
}
