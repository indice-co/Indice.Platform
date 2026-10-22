namespace Indice.Features.FtpServer.HealthChecks;

/// <summary>
/// Options for the <see cref="FtpServerHealthCheck"/>.
/// </summary>
public class FtpServerHealthCheckOptions
{
    /// <summary>
    /// When the free capacity (control connections or passive data ports) drops to or below this ratio (0..1),
    /// the check reports <see cref="Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded"/>. Defaults to <c>0</c>, meaning degraded only when fully exhausted.
    /// </summary>
    public double DegradedFreeCapacityRatio { get; set; } = 0;

    /// <summary>
    /// When <see langword="true"/> the check also inspects the operating system TCP table, so that ports in the passive range
    /// held by other processes (or lingering sockets) are not reported as free. Defaults to <see langword="false"/>.
    /// </summary>
    public bool InspectOperatingSystemPorts { get; set; }
}
