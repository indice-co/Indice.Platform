namespace Indice.Features.FtpServer.HealthChecks;

/// <summary>
/// A snapshot of the FTP server connection usage and the remaining capacity.
/// </summary>
public class FtpServerCapacity
{
    /// <summary>Gets a value indicating whether the server is ready to accept incoming connections.</summary>
    public bool Ready { get; init; }
    /// <summary>Gets the number of control connections currently open.</summary>
    public long ControlConnectionsActive { get; init; }
    /// <summary>Gets the total number of control connections since the server started.</summary>
    public long ControlConnectionsTotal { get; init; }
    /// <summary>Gets the maximum number of control connections allowed. <see langword="null"/> means unlimited.</summary>
    public int? ControlConnectionsMax { get; init; }
    /// <summary>Gets the number of control connections that can still be accepted. <see langword="null"/> means unlimited.</summary>
    public long? ControlConnectionsFree { get; init; }
    /// <summary>Gets the number of data connections (active or passive mode) currently open.</summary>
    public int DataConnectionsActive { get; init; }
    /// <summary>Gets the total number of data connections since the server started.</summary>
    public long DataConnectionsTotal { get; init; }
    /// <summary>Gets the number of passive listeners that are waiting for the client to connect.</summary>
    public int PassiveListenersPending { get; init; }
    /// <summary>Gets the number of times a passive listener could not be created (e.g. port range exhausted).</summary>
    public long PassiveListenerFailures { get; init; }
    /// <summary>Gets the lower bound of the passive port range. <see langword="null"/> when no range is configured.</summary>
    public int? PassivePortMin { get; init; }
    /// <summary>Gets the upper bound of the passive port range. <see langword="null"/> when no range is configured.</summary>
    public int? PassivePortMax { get; init; }
    /// <summary>Gets the number of ports in the passive port range. <see langword="null"/> when no range is configured (unlimited).</summary>
    public int? PassivePortsCapacity { get; init; }
    /// <summary>Gets the number of ports of the passive port range currently in use.</summary>
    public int? PassivePortsInUse { get; init; }
    /// <summary>Gets the number of new passive data connections that can be opened. <see langword="null"/> means unlimited.</summary>
    public int? PassivePortsFree { get; init; }

    /// <summary>
    /// Gets the number of new transfers that can be started right now, which is the minimum between the free control connections and free passive ports.
    /// <see langword="null"/> means unlimited.
    /// </summary>
    public long? Available {
        get {
            long? result = null;
            if (ControlConnectionsFree is long control) {
                result = control;
            }
            if (PassivePortsFree is int ports) {
                result = result is null ? ports : Math.Min(result.Value, ports);
            }
            return result;
        }
    }
}
