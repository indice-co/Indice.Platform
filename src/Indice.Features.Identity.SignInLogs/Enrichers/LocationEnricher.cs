using System.Net;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

internal class LocationEnricher : ISignInLogEntryEnricher
{
    public int Order => 6;
    public SignInLogEnricherRunType RunType => SignInLogEnricherRunType.Default;

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
