using System.Text;
using Indice.Features.Identity.Core.Logging.Models;
using MaxMind.GeoIP2;
using Microsoft.AspNetCore.Hosting;

namespace Indice.Features.Identity.Core.Logging.Enrichers;

internal class LocationEnricher : ISignInLogEntryEnricher
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public LocationEnricher(IWebHostEnvironment webHostEnvironment) {
        _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
    }

    public Task Enrich(SignInLogEntry logEntry) {
        if (string.IsNullOrWhiteSpace(logEntry?.IpAddress)) {
            return Task.CompletedTask;
        }
        var assembly = typeof(LocationEnricher).Assembly;
        var @namespace = assembly.GetName().Name;
        var citiesFileStream = assembly.GetManifestResourceStream($"{@namespace}.Logging.GeoLite2.{SignInLogOptions.GEO_LITE2_CITY_FILE_NAME}");
        var location = new StringBuilder();
        var shouldAddSeparator = false;
        const string separator = ", ";
        using (var reader = new DatabaseReader(citiesFileStream)) {
            if (reader.TryCity(logEntry.IpAddress, out var response)) {
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
        var countriesFileStream = assembly.GetManifestResourceStream($"{@namespace}.Logging.GeoLite2.{SignInLogOptions.GEO_LITE2_COUNTRY_FILE_NAME}");
        using (var reader = new DatabaseReader(countriesFileStream)) {
            if (reader.TryCountry(logEntry.IpAddress, out var response)) {
                if (!string.IsNullOrWhiteSpace(response?.Country?.Name)) {
                    if (shouldAddSeparator) {
                        location.Append(separator);
                    }
                    location.Append(response.Country.Name);
                    if (!string.IsNullOrWhiteSpace(response?.Country?.IsoCode)) {
                        location.AppendFormat(" ({0})", response.Country.IsoCode);
                    }
                }
            }
        }
        var locationString = location.ToString();
        logEntry.Location = locationString != string.Empty ? locationString : default;
        return Task.CompletedTask;
    }
}
