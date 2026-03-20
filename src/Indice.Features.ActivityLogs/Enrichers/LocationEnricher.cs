using System.Net;
using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Models;
using Indice.Features.GeoIP;

namespace Indice.Features.ActivityLogs.Enrichers;

/// <summary>Enriches the activity log entry with location metadata of the user (given the IP address).</summary>
public sealed class LocationEnricher : IActivityLogEntryEnricher
{
    /// <summary>Helper service for locating IP addresses</summary>
    private readonly IPAddressLocator _ipAddressLocator;

    /// <summary>Initializes a new instance of the <see cref="LocationEnricher"/> class.</summary>
    /// <param name="ipAddressLocator">The IP address locator service.</param>
    public LocationEnricher(IPAddressLocator ipAddressLocator) {
        _ipAddressLocator = ipAddressLocator ?? throw new ArgumentNullException(nameof(ipAddressLocator));
    }

    /// <inheritdoc />
    public int Order => 6;
    /// <inheritdoc />
    public ActivityLogEnricherRunType RunType => ActivityLogEnricherRunType.Default;

    /// <inheritdoc />
    public ValueTask EnrichAsync(ActivityLogEntry logEntry) {
        if (string.IsNullOrWhiteSpace(logEntry?.IpAddress)) {
            return ValueTask.CompletedTask;
        }
        var isValidIp = IPAddress.TryParse(logEntry.IpAddress, out var ipAddress);
        if (!isValidIp) {
            return ValueTask.CompletedTask;
        }
        var location = _ipAddressLocator.GetLocationMetadata(ipAddress!);
        logEntry.CountryIsoCode = location.CountryIsoCode;
        logEntry.Location = location.ToString();
        logEntry.Coordinates = location.Coordinates;
        return ValueTask.CompletedTask;
    }
}
