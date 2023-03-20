using Indice.Features.Identity.SignInLogs.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.SignInLogs.Hosting;

internal class LogCleanupHostedService : IHostedService
{
    private CancellationTokenSource _cts;
    private readonly SignInLogOptions _signInLogOptions;
    private readonly ILogger<LogCleanupHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public LogCleanupHostedService(
        IOptions<SignInLogOptions> signInLogOptions,
        ILogger<LogCleanupHostedService> logger,
        IServiceProvider serviceProvider
    ) {
        _signInLogOptions = signInLogOptions?.Value ?? throw new ArgumentNullException(nameof(signInLogOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    private TimeSpan CleanupInterval => TimeSpan.FromSeconds(_signInLogOptions.Cleanup.IntervalSeconds);

    public Task StartAsync(CancellationToken cancellationToken) {
        if (_cts is not null) {
            throw new InvalidOperationException($"{nameof(LogCleanupHostedService)} has already started. Call {nameof(StopAsync)} first.");
        }
        _logger.LogDebug($"{nameof(LogCleanupHostedService)} is starting.");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task.Factory.StartNew(() => StartInternalAsync(_cts.Token), cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        if (_cts is null) {
            throw new InvalidOperationException($"{nameof(LogCleanupHostedService)} has not started. Call {nameof(StartAsync)} first.");
        }
        _logger.LogDebug($"{nameof(LogCleanupHostedService)} is stopping.");
        _cts.Cancel();
        _cts = null;
        return Task.CompletedTask;
    }

    private async Task StartInternalAsync(CancellationToken cancellationToken) {
        while (true) {
            if (cancellationToken.IsCancellationRequested) {
                _logger.LogDebug("Cancellation was requested for {ServiceName}. Exiting.", nameof(LogCleanupHostedService));
                break;
            }
            try {
                await Task.Delay(CleanupInterval, cancellationToken);
            } catch (TaskCanceledException) {
                _logger.LogDebug("{ExceptionName}. Exiting.", nameof(TaskCanceledException));
                break;
            } catch (Exception exception) {
                _logger.LogError("Task.Delay exception: {Exception}. Exiting.", exception.Message);
                break;
            }
            if (cancellationToken.IsCancellationRequested) {
                _logger.LogDebug("Cancellation was requested for {ServiceName}. Exiting.", nameof(LogCleanupHostedService));
                break;
            }
            await RemoveOldLogsAsync(cancellationToken);
        }
    }

    private async Task RemoveOldLogsAsync(CancellationToken cancellationToken = default) {
        try {
            using var serviceScope = _serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var signInLogStore = serviceScope.ServiceProvider.GetRequiredService<ISignInLogStore>();
            await signInLogStore.Cleanup(cancellationToken);
        } catch (Exception exception) {
            _logger.LogError("Exception while removing expired logs: {Exception}", exception.Message);
        }
    }
}
