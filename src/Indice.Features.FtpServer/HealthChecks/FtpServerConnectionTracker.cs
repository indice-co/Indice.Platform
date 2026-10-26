using System.Collections.Concurrent;
using System.Linq;
using FubarDev.FtpServer;

namespace Indice.Features.FtpServer.HealthChecks;

/// <summary>
/// Keeps track of the passive (PASV/EPSV) listeners and the data connections that are currently open on the FTP server.
/// </summary>
/// <remarks>
/// Registered as a singleton by <c>EnableConnectionTracking</c>. Counts are per process (per server instance).
/// </remarks>
public sealed class FtpServerConnectionTracker
{
    private readonly ConcurrentDictionary<object, int> _passiveListeners = new(ReferenceEqualityComparer.Instance);
    private readonly ConcurrentDictionary<IFtpDataConnection, byte> _dataConnections = new(ReferenceEqualityComparer.Instance);
    private long _totalDataConnections;
    private long _passiveListenerFailures;

    /// <summary>
    /// Gets the number of passive listeners that are waiting for the client to open the data connection.
    /// </summary>
    public int PendingPassiveListeners => _passiveListeners.Count;

    /// <summary>
    /// Gets the number of data connections (active or passive mode) that are currently open.
    /// </summary>
    public int ActiveDataConnections {
        get {
            PruneClosedDataConnections();
            return _dataConnections.Count;
        }
    }

    /// <summary>
    /// Gets the total number of data connections that were established since the server started.
    /// </summary>
    public long TotalDataConnections => Interlocked.Read(ref _totalDataConnections);

    /// <summary>
    /// Gets the number of times a passive listener could not be created (e.g. no free port in the passive port range).
    /// </summary>
    public long PassiveListenerFailures => Interlocked.Read(ref _passiveListenerFailures);

    /// <summary>
    /// Counts the distinct local ports, within the given range, that are held by tracked passive listeners and open data connections.
    /// </summary>
    /// <param name="minPort">The lower bound of the port range (inclusive).</param>
    /// <param name="maxPort">The upper bound of the port range (inclusive).</param>
    /// <returns>The number of ports in use.</returns>
    public int CountPortsInUse(int minPort, int maxPort) => GetPortsInUse(minPort, maxPort).Count;

    /// <summary>
    /// Gets the distinct local ports, within the given range, that are held by tracked passive listeners and open data connections.
    /// </summary>
    /// <param name="minPort">The lower bound of the port range (inclusive).</param>
    /// <param name="maxPort">The upper bound of the port range (inclusive).</param>
    /// <returns>The set of ports in use.</returns>
    public IReadOnlySet<int> GetPortsInUse(int minPort, int maxPort) {
        PruneClosedDataConnections();
        var ports = new HashSet<int>();
        foreach (var port in _passiveListeners.Values.Where(port => port >= minPort && port <= maxPort)) {
            ports.Add(port);
        }
        foreach (var connection in _dataConnections.Keys) {
            var port = connection.LocalAddress?.Port ?? 0;
            if (port >= minPort && port <= maxPort) {
                ports.Add(port);
            }
        }
        return ports;
    }

    internal void PassiveListenerCreated(object listener, int port) => _passiveListeners.TryAdd(listener, port);

    internal void PassiveListenerDisposed(object listener) => _passiveListeners.TryRemove(listener, out _);

    internal void PassiveListenerFailed() => Interlocked.Increment(ref _passiveListenerFailures);

    internal void DataConnectionOpened(IFtpDataConnection connection) {
        PruneClosedDataConnections();
        if (_dataConnections.TryAdd(connection, 0)) {
            Interlocked.Increment(ref _totalDataConnections);
        }
    }

    private void PruneClosedDataConnections() {
        foreach (var connection in _dataConnections.Keys.Where(connection => !IsOpen(connection))) {
            _dataConnections.TryRemove(connection, out _);
        }
    }

    private static bool IsOpen(IFtpDataConnection connection) {
        if (connection.Closed) {
            return false;
        }
        try {
            // A disposed stream reports that it can neither read nor write.
            var stream = connection.Stream;
            return stream.CanRead || stream.CanWrite;
        } catch (ObjectDisposedException) {
            return false;
        }
    }
}
