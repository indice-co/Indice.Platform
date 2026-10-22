using System.Net.NetworkInformation;
using FubarDev.FtpServer;
using Microsoft.Extensions.Options;

namespace Indice.Features.FtpServer.HealthChecks;

/// <summary>
/// Provides a snapshot of the FTP server connection usage and the remaining capacity.
/// </summary>
public interface IFtpServerCapacityProvider
{
    /// <summary>
    /// Gets the current capacity snapshot.
    /// </summary>
    /// <param name="inspectOperatingSystemPorts">
    /// When <see langword="true"/> the operating system TCP table is also inspected so that ports of the passive range used by other sockets are taken into account.
    /// </param>
    /// <returns>The capacity snapshot.</returns>
    FtpServerCapacity GetCapacity(bool inspectOperatingSystemPorts = false);
}

/// <summary>
/// Default implementation of <see cref="IFtpServerCapacityProvider"/>.
/// </summary>
public class FtpServerCapacityProvider : IFtpServerCapacityProvider
{
    private readonly IFtpServer _ftpServer;
    private readonly FtpServerConnectionTracker _tracker;
    private readonly IOptions<SimplePasvOptions> _pasvOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="FtpServerCapacityProvider"/> class.
    /// </summary>
    /// <param name="ftpServer">The FTP server.</param>
    /// <param name="tracker">The connection tracker.</param>
    /// <param name="pasvOptions">The passive mode options.</param>
    public FtpServerCapacityProvider(IFtpServer ftpServer, FtpServerConnectionTracker tracker, IOptions<SimplePasvOptions> pasvOptions) {
        _ftpServer = ftpServer ?? throw new ArgumentNullException(nameof(ftpServer));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _pasvOptions = pasvOptions ?? throw new ArgumentNullException(nameof(pasvOptions));
    }

    /// <inheritdoc />
    public FtpServerCapacity GetCapacity(bool inspectOperatingSystemPorts = false) {
        var statistics = _ftpServer.Statistics;
        var controlActive = statistics.ActiveConnections;
        var controlMax = _ftpServer.MaxActiveConnections > 0 ? _ftpServer.MaxActiveConnections : (int?)null;

        var (minPort, maxPort) = ResolvePassivePortRange(_pasvOptions.Value);
        int? capacity = null, inUse = null, free = null;
        if (minPort is int min && maxPort is int max) {
            capacity = max - min + 1;
            inUse = inspectOperatingSystemPorts ? CountPortsInUseIncludingOperatingSystem(min, max) : _tracker.CountPortsInUse(min, max);
            free = Math.Max(0, capacity.Value - inUse.Value);
        }

        return new FtpServerCapacity {
            Ready = _ftpServer.Ready,
            ControlConnectionsActive = controlActive,
            ControlConnectionsTotal = statistics.TotalConnections,
            ControlConnectionsMax = controlMax,
            ControlConnectionsFree = controlMax is int limit ? Math.Max(0, limit - controlActive) : null,
            DataConnectionsActive = _tracker.ActiveDataConnections,
            DataConnectionsTotal = _tracker.TotalDataConnections,
            PassiveListenersPending = _tracker.PendingPassiveListeners,
            PassiveListenerFailures = _tracker.PassiveListenerFailures,
            PassivePortMin = minPort,
            PassivePortMax = maxPort,
            PassivePortsCapacity = capacity,
            PassivePortsInUse = inUse,
            PassivePortsFree = free
        };
    }

    // Mirrors the port normalization performed by FubarDev.FtpServer.SimplePasvAddressResolver.
    private static (int? Min, int? Max) ResolvePassivePortRange(SimplePasvOptions options) {
        var min = options.PasvMinPort.GetValueOrDefault();
        if (min > 0 && min < 1024) {
            min = 1024;
        }
        var max = Math.Max(options.PasvMaxPort.GetValueOrDefault(), min);
        if (min == 0 && max == 0) {
            return (null, null);
        }
        return (min, max);
    }

    private int CountPortsInUseIncludingOperatingSystem(int minPort, int maxPort) {
        var ports = new HashSet<int>();
        var properties = IPGlobalProperties.GetIPGlobalProperties();
        foreach (var endpoint in properties.GetActiveTcpListeners()) {
            if (endpoint.Port >= minPort && endpoint.Port <= maxPort) {
                ports.Add(endpoint.Port);
            }
        }
        foreach (var connection in properties.GetActiveTcpConnections()) {
            var port = connection.LocalEndPoint.Port;
            if (port >= minPort && port <= maxPort && connection.State is not (TcpState.TimeWait or TcpState.Closed)) {
                ports.Add(port);
            }
        }
        var tracked = _tracker.CountPortsInUse(minPort, maxPort);
        return Math.Max(ports.Count, tracked);
    }
}
