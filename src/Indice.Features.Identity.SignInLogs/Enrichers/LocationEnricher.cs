using System.Text;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;
using Indice.Types;
using MaxMind.GeoIP2;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

internal class LocationEnricher : ISignInLogEntryEnricher
{
    public int Order => 5;

    public Task Enrich(SignInLogEntry logEntry) {
        if (string.IsNullOrWhiteSpace(logEntry?.IpAddress)) {
            return Task.CompletedTask;
        }
        const string separator = ", ";
        var assembly = typeof(LocationEnricher).Assembly;
        var @namespace = assembly.GetName().Name;
        var citiesFileStream = assembly.GetManifestResourceStream($"{@namespace}.GeoLite2.{SignInLogOptions.GEO_LITE2_CITY_FILE_NAME}");
        var location = new StringBuilder();
        var shouldAddSeparator = false;
        double? latitude = null, longitude = null;
        using (var reader = new DatabaseReader(citiesFileStream)) {
            if (reader.TryCity(logEntry.IpAddress, out var response)) {
                latitude = response?.Location?.Latitude;
                longitude = response?.Location?.Longitude;
                if (!string.IsNullOrWhiteSpace(response?.City?.Name)) {
                    location.Append(response.City.Name);
                    shouldAddSeparator = true;
                    if (!string.IsNullOrWhiteSpace(response?.Postal?.Code)) {
                        location.AppendFormat(" {0}", response.Postal.Code);
                    }
                }
                if (response?.Subdivisions?.Any() == true) {
                    if (shouldAddSeparator) {
                        location.Append(separator);
                    }
                    location.AppendJoin(" | ", response.Subdivisions);
                    shouldAddSeparator = true;
                }
            }
        }
        var countriesFileStream = assembly.GetManifestResourceStream($"{@namespace}.GeoLite2.{SignInLogOptions.GEO_LITE2_COUNTRY_FILE_NAME}");
        string countryCode = null;
        using (var reader = new DatabaseReader(countriesFileStream)) {
            if (reader.TryCountry(logEntry.IpAddress, out var response)) {
                if (!string.IsNullOrWhiteSpace(response?.Country?.Name)) {
                    if (shouldAddSeparator) {
                        location.Append(separator);
                    }
                    location.Append(response.Country.Name);
                    if (!string.IsNullOrWhiteSpace(response?.Country?.IsoCode)) {
                        countryCode = response.Country?.IsoCode;
                        location.AppendFormat(" ({0})", countryCode);
                    }
                }
            }
        }
        var locationString = location.ToString();
        logEntry.CountryIsoCode = countryCode;
        logEntry.Location = locationString != string.Empty ? locationString : default;
        if (latitude.HasValue && longitude.HasValue) {
            logEntry.Coordinates = new GeoPoint(latitude.Value, longitude.Value);
        }
        return Task.CompletedTask;
    }
}
