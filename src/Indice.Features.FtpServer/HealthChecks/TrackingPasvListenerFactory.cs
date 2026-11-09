using System.Net;
using System.Net.Sockets;
using FubarDev.FtpServer;

namespace Indice.Features.FtpServer.HealthChecks;

/// <summary>
/// Decorates an <see cref="IPasvListenerFactory"/> in order to keep track of the passive listeners created.
/// </summary>
internal sealed class TrackingPasvListenerFactory : IPasvListenerFactory
{
    private readonly IPasvListenerFactory _inner;
    private readonly FtpServerConnectionTracker _tracker;

    public TrackingPasvListenerFactory(IPasvListenerFactory inner, FtpServerConnectionTracker tracker) {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
    }

    public async Task<IPasvListener> CreateTcpListenerAsync(IFtpConnection connection, AddressFamily? addressFamily = null, int port = 0, CancellationToken cancellationToken = default) {
        IPasvListener listener;
        try {
            listener = await _inner.CreateTcpListenerAsync(connection, addressFamily, port, cancellationToken).ConfigureAwait(false);
        } catch (SocketException) {
            _tracker.PassiveListenerFailed();
            throw;
        }
        return new TrackedPasvListener(listener, _tracker);
    }

    private sealed class TrackedPasvListener : IPasvListener
    {
        private readonly IPasvListener _inner;
        private readonly FtpServerConnectionTracker _tracker;
        private int _disposed;

        public TrackedPasvListener(IPasvListener inner, FtpServerConnectionTracker tracker) {
            _inner = inner;
            _tracker = tracker;
            _tracker.PassiveListenerCreated(this, inner.PasvEndPoint.Port);
        }

        public IPEndPoint PasvEndPoint => _inner.PasvEndPoint;

        public Task<TcpClient> AcceptPasvClientAsync() => _inner.AcceptPasvClientAsync();

        public void Dispose() {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) {
                _tracker.PassiveListenerDisposed(this);
            }
            _inner.Dispose();
        }
    }
}
