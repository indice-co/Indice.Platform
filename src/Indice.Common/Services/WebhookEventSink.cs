using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Indice.Services;

/// <summary>Processes webhook events in the background.</summary>
public sealed class WebhookEventSink : BackgroundService
{
    private readonly ChannelReader<List<WebhookEvent>> _channelReader;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebhookEventSink> _logger;
    private readonly CancellationTokenSource _drainCts = new();

    /// <summary>Initializes a new instance of the <see cref="WebhookEventSink"/> class.</summary>
    public WebhookEventSink(
        Channel<List<WebhookEvent>> channel,
        IServiceProvider serviceProvider,
        ILogger<WebhookEventSink> logger) {
        _channelReader = channel.Reader;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        var drainToken = _drainCts.Token;
        try {
            while (await _channelReader.WaitToReadAsync(stoppingToken)) {
                await DrainAsync(drainToken);
            }
        } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }

        try {
            await DrainAsync(drainToken);
        } catch (OperationCanceledException) when (drainToken.IsCancellationRequested) {
            _logger.LogWarning("Shutdown timeout reached before the webhook queue was fully drained.");
        }
    }

    /// <inheritdoc/>
    public override async Task StopAsync(CancellationToken cancellationToken) {
        using var registration = cancellationToken.Register(static state => ((CancellationTokenSource)state!).Cancel(), _drainCts);
        await base.StopAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override void Dispose() {
        _drainCts.Dispose();
        base.Dispose();
    }

    private async Task DrainAsync(CancellationToken cancellationToken) {
        while (_channelReader.TryRead(out var batch)) {
            await ProcessAsync(batch, cancellationToken);
        }
    }

    private async Task ProcessAsync(List<WebhookEvent> batch, CancellationToken cancellationToken) {
        try {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IWebhookHandler>();
            await handler.HandleAsync(batch, cancellationToken);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to handle webhook batch of {Count} events", batch.Count);
        }
    }
}