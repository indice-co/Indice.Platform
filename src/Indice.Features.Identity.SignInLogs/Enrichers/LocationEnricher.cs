using System.Net;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

/// <summary>Enriches the sign in log entry with location metadata of the user (given the IP address).</summary>
public sealed class LocationEnricher : ISignInLogEntryEnricher
{
    /// <inheritdoc />
    public int Order => 6;
    /// <inheritdoc />
    public SignInLogEnricherRunType RunType => SignInLogEnricherRunType.Default;

    /// <inheritdoc />
    public ValueTask EnrichAsync(SignInLogEntry logEntry) {
        if (string.IsNullOrWhiteSpace(logEntry?.IpAddress)) {
            return ValueTask.CompletedTask;
        }
        var isValidIp = IPAddress.TryParse(logEntry.IpAddress, out var ipAddress);
        if (!isValidIp) {
            return ValueTask.CompletedTask;
        }
        var location = ipAddress.GetLocationMetadata();
        logEntry.CountryIsoCode = location.CountryIsoCode;
        logEntry.Location = location.ToString();
        logEntry.Coordinates = location.Coordinates;
        return ValueTask.CompletedTask;
    }
}
