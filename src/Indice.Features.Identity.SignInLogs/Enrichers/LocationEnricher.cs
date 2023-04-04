using System.Net;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

internal class LocationEnricher : ISignInLogEntryEnricher
{
    public int Priority => 5;
    public EnricherDependencyType DependencyType => EnricherDependencyType.Default;

    public Task EnrichAsync(SignInLogEntry logEntry) {
        if (string.IsNullOrWhiteSpace(logEntry?.IpAddress)) {
            return Task.CompletedTask;
        }
        var isValidIp = IPAddress.TryParse(logEntry.IpAddress, out var ipAddress);
        if (!isValidIp) {
            return Task.CompletedTask;
        }
        var location = ipAddress.GetLocationMetadata();
        logEntry.CountryIsoCode = location.CountryIsoCode;
        logEntry.Location = location.ToString();
        logEntry.Coordinates = location.Coordinates;
        return Task.CompletedTask;
    }
}
