using System.Text;
using Indice.Features.Identity.SignInLogs;
using Indice.Types;
using MaxMind.GeoIP2;

namespace System.Net;

/// <summary>Extension methods on <see cref="IPAddress"/> type.</summary>
public static class IPAddressExtensions
{
    /// <summary>Gets various geolocation data for the given <see cref="IPAddress"/>.</summary>
    /// <param name="ipAddress">The IP address to look for.</param>
    public static IPLocationMetadata GetLocationMetadata(this IPAddress ipAddress) {
        var assembly = typeof(IPAddressExtensions).Assembly;
        var @namespace = assembly.GetName().Name;
        var result = new IPLocationMetadata();
        var citiesFileStream = assembly.GetManifestResourceStream($"{@namespace}.GeoLite2.{SignInLogOptions.GEO_LITE2_CITY_FILE_NAME}");
        using (var reader = new DatabaseReader(citiesFileStream)) {
            if (reader.TryCity(ipAddress, out var response)) {
                var latitude = response?.Location?.Latitude;
                var longitude = response?.Location?.Longitude;
                if (latitude.HasValue && longitude.HasValue) {
                    result.Coordinates = new GeoPoint(latitude.Value, longitude.Value);
                }
                result.CityName = response?.City?.Name;
                result.PostalCode = response?.Postal?.Code;
                if (response?.Subdivisions?.Any() == true) {
                    result.Subdivisions.AddRange(response.Subdivisions.Select(subdivision => subdivision.Name));
                }
            }
        }
        var countriesFileStream = assembly.GetManifestResourceStream($"{@namespace}.GeoLite2.{SignInLogOptions.GEO_LITE2_COUNTRY_FILE_NAME}");
        using (var reader = new DatabaseReader(countriesFileStream)) {
            if (reader.TryCountry(ipAddress, out var response)) {
                result.CountryName = response?.Country?.Name;
                result.CountryIsoCode = response?.Country?.IsoCode;
                result.Continent = response?.Continent?.Name;
            }
        }
        return result;
    }
}

/// <summary>Models the geolocation data that were retrieved by the incoming IP address.</summary>
public class IPLocationMetadata
{
    /// <summary>The city name.</summary>
    public string CityName { get; set; }
    /// <summary>Subdivisions.</summary>
    public List<string> Subdivisions { get; set; } = new List<string>();
    /// <summary>The country name.</summary>
    public string CountryName { get; set; }
    /// <summary>The country two letter ISO code.</summary>
    public string CountryIsoCode { get; set; }
    /// <summary>The postal code.</summary>
    public string PostalCode { get; set; }
    /// <summary>The continent name.</summary>
    public string Continent { get; set; }
    /// <summary></summary>
    public GeoPoint Coordinates { get; set; }

    /// <inheritdoc/>
    public override string ToString() {
        const string separator = ", ";
        var locationBuilder = new StringBuilder();
        var shouldAddSeparator = false;
        if (!string.IsNullOrWhiteSpace(CityName)) {
            locationBuilder.Append(CityName);
            shouldAddSeparator = true;
            if (!string.IsNullOrWhiteSpace(PostalCode)) {
                locationBuilder.AppendFormat(" {0}", PostalCode);
            }
        }
        if (Subdivisions.Any()) {
            if (shouldAddSeparator) {
                locationBuilder.Append(separator);
            }
            locationBuilder.AppendJoin(" | ", Subdivisions);
            shouldAddSeparator = true;
        }
        if (!string.IsNullOrWhiteSpace(CountryName)) {
            if (shouldAddSeparator) {
                locationBuilder.Append(separator);
            }
            locationBuilder.Append(CountryName);
            shouldAddSeparator = true;
        }
        if (!string.IsNullOrWhiteSpace(Continent)) {
            if (shouldAddSeparator) {
                locationBuilder.Append(separator);
            }
            locationBuilder.Append(Continent);
        }
        return locationBuilder.ToString();
    }
}
