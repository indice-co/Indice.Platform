using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace Indice.Features.Agents.Core.Extensions;

/// <summary>
/// Defines a factory for creating instances of <see cref="McpClient"/>.
/// </summary>
public interface IMcpClientFactory
{
    /// <summary>
    /// Creates a new instance of <see cref="McpClient"/> asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created <see cref="McpClient"/>.</returns>
    Task<McpClient> CreateAsync(CancellationToken cancellationToken = default);
}

internal sealed class McpClientFactory(
    IServiceProvider services,
    McpClientRegistrationOptions options,
    ILoggerFactory? loggerFactory) : IMcpClientFactory, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private McpClient? _shared;
    private bool _disposed;

    public async Task<McpClient> CreateAsync(CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!options.ShareSession)
            return await CreateCoreAsync(cancellationToken).ConfigureAwait(false);

        if (_shared is not null)
            return _shared;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _shared ??= await CreateCoreAsync(cancellationToken).ConfigureAwait(false);
        } finally {
            _gate.Release();
        }
    }

    private Task<McpClient> CreateCoreAsync(CancellationToken cancellationToken) {
        IClientTransport transport = options.TransportFactory(services);
        return McpClient.CreateAsync(transport, options.Client, loggerFactory, cancellationToken);
    }

    public async ValueTask DisposeAsync() {
        if (_disposed) return;
        _disposed = true;
        if (_shared is not null)
            await _shared.DisposeAsync().ConfigureAwait(false);
        _gate.Dispose();
    }
}
