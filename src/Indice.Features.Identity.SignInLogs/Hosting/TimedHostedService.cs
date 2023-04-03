using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Hosting;

/// <summary>An <see cref="IHostedService"/> that is executed periodically at a specified interval.</summary>
public abstract class TimedHostedService : IHostedService, IDisposable
{
    private bool _disposed = false;
    private CancellationTokenSource _stoppingCts;

    /// <summary>Creates a new instance of <see cref="TimedHostedService"/> class.</summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public TimedHostedService(ILogger logger) => Logger = logger;

    /// <summary>The <see cref="TimeSpan"/> used </summary>
    public abstract TimeSpan Interval { get; }
    /// <summary>Represents a type used to perform logging.</summary>
    public ILogger Logger { get; }

    /// <summary>This method is called when the <see cref="IHostedService"/> starts. The implementation should return a task that represents the lifetime of the long running operation(s) being performed.</summary>
    /// <param name="stoppingToken">Triggered when <see cref="IHostedService.StopAsync(CancellationToken)"/> is called.</param>
    /// <returns>A <see cref="Task"/> that represents the long running operations.</returns>
    /// <remarks>See <see href="https://docs.microsoft.com/dotnet/core/extensions/workers">Worker Services in .NET</see> for implementation guidelines.</remarks>
    protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

    /// <summary>Triggered when the application host is ready to start the service.</summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous Start operation.</returns>
    public Task StartAsync(CancellationToken cancellationToken) {
        // Create linked token to allow canceling executing task from provided token.
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task.Factory.StartNew(async () => {
            while (true) {
                if (_stoppingCts.Token.IsCancellationRequested) {
                    Logger.LogDebug("Cancellation was requested for TimedHostedService. Exiting.");
                    break;
                }
                try {
                    await Task.Delay(Interval, _stoppingCts.Token);
                    await ExecuteAsync(_stoppingCts.Token);
                } catch (TaskCanceledException exception) {
                    Logger.LogDebug("A TimedHostedService was canceled. {Exception}", exception.Message);
                    break;
                } catch (Exception exception) {
                    Logger.LogDebug("An exception was thrown during TimedHostedService execution. {Exception}", exception.Message);
                    break;
                }
            }
        }, cancellationToken);
        return Task.CompletedTask;
    }

    /// <summary>Triggered when the application host is performing a graceful shutdown.</summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous Stop operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken) {
        // Signal cancellation to the executing method.
        _stoppingCts?.Cancel();
        _stoppingCts = null;
        return Task.CompletedTask;
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    /// <param name="disposing">Indicates whether the method call comes from a <see cref="Dispose(bool)"/> method (its value is <see langword="true"/>) or from a finalizer (its value is <see langword="false"/>).</param>
    protected virtual void Dispose(bool disposing) {
        if (!_disposed) {
            if (disposing) {
                // Called via myClass.Dispose(). 
                // OK to use any private object references.
                _stoppingCts?.Cancel();
            }
            // Release unmanaged resources.
            // Set large fields to null.                
            _disposed = true;
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Class finalizer.</summary>
    ~TimedHostedService() {
        Dispose(false);
    }
}