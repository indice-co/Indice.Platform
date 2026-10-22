using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Indice.Features.FtpServer.HealthChecks;

/// <summary>
/// Health check that reports the FTP server readiness along with the connection usage and the remaining capacity.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><see cref="HealthStatus.Unhealthy"/> when the server is not ready to accept connections.</item>
///   <item><see cref="HealthStatus.Degraded"/> when the free control connections or free passive ports drop to or below <see cref="FtpServerHealthCheckOptions.DegradedFreeCapacityRatio"/>.</item>
///   <item><see cref="HealthStatus.Healthy"/> otherwise.</item>
/// </list>
/// </remarks>
public class FtpServerHealthCheck : IHealthCheck
{
    private readonly IFtpServerCapacityProvider _capacityProvider;
    private readonly IOptionsMonitor<FtpServerHealthCheckOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="FtpServerHealthCheck"/> class.
    /// </summary>
    /// <param name="capacityProvider">The capacity provider.</param>
    /// <param name="options">The health check options.</param>
    public FtpServerHealthCheck(IFtpServerCapacityProvider capacityProvider, IOptionsMonitor<FtpServerHealthCheckOptions> options) {
        _capacityProvider = capacityProvider ?? throw new ArgumentNullException(nameof(capacityProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
        var options = _options.Get(context.Registration.Name);
        FtpServerCapacity capacity;
        try {
            capacity = _capacityProvider.GetCapacity(options.InspectOperatingSystemPorts);
        } catch (Exception ex) {
            return Task.FromResult(new HealthCheckResult(context.Registration.FailureStatus, "Could not determine the FTP server capacity.", ex));
        }
        var data = ToDictionary(capacity);
        if (!capacity.Ready) {
            return Task.FromResult(new HealthCheckResult(context.Registration.FailureStatus, "FTP server is not ready to accept connections.", data: data));
        }
        var reasons = new List<string>();
        if (IsLow(capacity.ControlConnectionsFree, capacity.ControlConnectionsMax, options.DegradedFreeCapacityRatio)) {
            reasons.Add($"control connections free {capacity.ControlConnectionsFree}/{capacity.ControlConnectionsMax}");
        }
        if (IsLow(capacity.PassivePortsFree, capacity.PassivePortsCapacity, options.DegradedFreeCapacityRatio)) {
            reasons.Add($"passive ports free {capacity.PassivePortsFree}/{capacity.PassivePortsCapacity}");
        }
        if (reasons.Count > 0) {
            return Task.FromResult(HealthCheckResult.Degraded($"FTP server is running low on capacity: {string.Join(", ", reasons)}.", data: data));
        }
        return Task.FromResult(HealthCheckResult.Healthy($"FTP server is ready. Available: {capacity.Available?.ToString() ?? "unlimited"}.", data));
    }

    private static bool IsLow(long? free, long? total, double ratio) {
        if (free is null || total is null || total <= 0) {
            return false;
        }
        return free.Value <= 0 || (double)free.Value / total.Value <= ratio;
    }

    private static IReadOnlyDictionary<string, object> ToDictionary(FtpServerCapacity capacity) {
        var data = new Dictionary<string, object> {
            ["ready"] = capacity.Ready,
            ["controlConnectionsActive"] = capacity.ControlConnectionsActive,
            ["controlConnectionsTotal"] = capacity.ControlConnectionsTotal,
            ["dataConnectionsActive"] = capacity.DataConnectionsActive,
            ["dataConnectionsTotal"] = capacity.DataConnectionsTotal,
            ["passiveListenersPending"] = capacity.PassiveListenersPending,
            ["passiveListenerFailures"] = capacity.PassiveListenerFailures
        };
        AddIfNotNull(data, "controlConnectionsMax", capacity.ControlConnectionsMax);
        AddIfNotNull(data, "controlConnectionsFree", capacity.ControlConnectionsFree);
        AddIfNotNull(data, "passivePortMin", capacity.PassivePortMin);
        AddIfNotNull(data, "passivePortMax", capacity.PassivePortMax);
        AddIfNotNull(data, "passivePortsCapacity", capacity.PassivePortsCapacity);
        AddIfNotNull(data, "passivePortsInUse", capacity.PassivePortsInUse);
        AddIfNotNull(data, "passivePortsFree", capacity.PassivePortsFree);
        AddIfNotNull(data, "available", capacity.Available);
        return data;
    }

    private static void AddIfNotNull(Dictionary<string, object> data, string key, object? value) {
        if (value is not null) {
            data[key] = value;
        }
    }
}
