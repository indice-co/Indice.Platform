using System.Net;
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
        var properties = IPGlobalProperties.GetIPGlobalProperties();
        var listeners = properties.GetActiveTcpListeners();
        var bindAddresses = ResolvePassiveBindAddresses(listeners);
        var ports = new HashSet<int>();
        ports.UnionWith(listeners
            .Where(endpoint => endpoint.Port >= minPort && endpoint.Port <= maxPort && ConflictsWithAny(endpoint.Address, bindAddresses))
            .Select(endpoint => endpoint.Port));
        // Connections in TIME_WAIT are counted as well, since they may prevent the passive listener from binding to the port
        // (e.g. when the socket was not opened with address reuse). This may underestimate the free ports where reuse is supported.
        ports.UnionWith(properties.GetActiveTcpConnections()
            .Where(connection => connection.State != TcpState.Closed)
            .Select(connection => connection.LocalEndPoint)
            .Where(endpoint => endpoint.Port >= minPort && endpoint.Port <= maxPort && ConflictsWithAny(endpoint.Address, bindAddresses))
            .Select(endpoint => endpoint.Port));
        var tracked = _tracker.CountPortsInUse(minPort, maxPort);
        return Math.Max(ports.Count, tracked);
    }

    // Passive listeners bind to the local address of the control connection, so only the addresses accepted by the FTP control listener are relevant.
    // When the control listener cannot be found, every address is considered to be a possible bind address.
    private IPAddress[] ResolvePassiveBindAddresses(IEnumerable<IPEndPoint> listeners) {
        var controlPort = _ftpServer.Port;
        var addresses = controlPort > 0
            ? listeners.Where(endpoint => endpoint.Port == controlPort).Select(endpoint => Normalize(endpoint.Address)).Distinct().ToArray()
            : [];
        return addresses.Length > 0 ? addresses : [IPAddress.Any, IPAddress.IPv6Any];
    }

    private static bool ConflictsWithAny(IPAddress address, IPAddress[] bindAddresses) => bindAddresses.Any(bindAddress => AddressesConflict(bindAddress, address));

    private static bool AddressesConflict(IPAddress bindAddress, IPAddress otherAddress) {
        bindAddress = Normalize(bindAddress);
        otherAddress = Normalize(otherAddress);
        if (bindAddress.AddressFamily == otherAddress.AddressFamily) {
            return IsWildcard(bindAddress) || IsWildcard(otherAddress) || bindAddress.Equals(otherAddress);
        }
        // An IPv6 wildcard socket may be dual-stack and therefore also hold the port for the IPv4 addresses.
        return IPAddress.IPv6Any.Equals(bindAddress) || IPAddress.IPv6Any.Equals(otherAddress);
    }

    private static bool IsWildcard(IPAddress address) => IPAddress.Any.Equals(address) || IPAddress.IPv6Any.Equals(address);

    private static IPAddress Normalize(IPAddress address) => address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
}
