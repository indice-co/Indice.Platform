using Indice.Features.Identity.SignInLogs.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.SignInLogs.Hosting;

internal class LogCleanupHostedService : BackgroundService
{
    private readonly ILogger<LogCleanupHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly SignInLogOptions _signInLogOptions;

    public LogCleanupHostedService(
        ILogger<LogCleanupHostedService> logger,
        IServiceProvider serviceProvider,
        IOptions<SignInLogOptions> signInLogOptions
    ) {
        _signInLogOptions = signInLogOptions?.Value ?? throw new ArgumentNullException(nameof(signInLogOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public TimeSpan Interval => TimeSpan.FromSeconds(_signInLogOptions.Cleanup.IntervalSeconds);

    protected override Task ExecuteAsync(CancellationToken stoppingToken) {
        Task.Factory.StartNew(async () => {
            while (true) {
                if (stoppingToken.IsCancellationRequested) {
                    _logger.LogDebug("Cancellation was requested for {ServiceName}. Exiting.", nameof(LogCleanupHostedService));
                    break;
                }
                try {
                    await Task.Delay(Interval, stoppingToken);
                    try {
                        using (var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
                            var signInLogStore = serviceScope.ServiceProvider.GetRequiredService<ISignInLogStore>();
                            await signInLogStore.Cleanup(stoppingToken);
                        }
                    } catch (Exception exception) {
                        _logger.LogError("Exception while removing expired logs: {Exception}", exception.Message);
                    }
                } catch (TaskCanceledException exception) {
                    _logger.LogDebug("{ServiceName} was canceled. {Exception}", nameof(LogCleanupHostedService), exception.Message);
                    break;
                } catch (Exception exception) {
                    _logger.LogDebug("An exception was thrown during {ServiceName} execution. {Exception}", nameof(LogCleanupHostedService), exception.Message);
                    break;
                }
            }
        }, stoppingToken);
        return Task.CompletedTask;
    }
}
