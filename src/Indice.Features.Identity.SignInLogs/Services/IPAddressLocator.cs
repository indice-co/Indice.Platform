using System.Net;
using System.Text;
using Indice.Features.Identity.SignInLogs.GeoLite2;
using Indice.Types;

namespace Indice.Features.Identity.SignInLogs.Services;

/// <summary></summary>
public sealed class IPAddressLocator
{
    private readonly CityDatabaseReader _cityDatabaseReader;
    private readonly CountryDatabaseReader _countryDatabaseReader;

    /// <summary></summary>
    /// <param name="cityDatabaseReader"></param>
    /// <param name="countryDatabaseReader"></param>
    public IPAddressLocator(CityDatabaseReader cityDatabaseReader, CountryDatabaseReader countryDatabaseReader) {
        _cityDatabaseReader = cityDatabaseReader ?? throw new ArgumentNullException(nameof(cityDatabaseReader));
        _countryDatabaseReader = countryDatabaseReader ?? throw new ArgumentNullException(nameof(countryDatabaseReader));
    }

    /// <summary>Gets various geolocation data for the given <see cref="IPAddress"/>.</summary>
    /// <param name="ipAddress">The IP address to look for.</param>
    public IPLocationMetadata GetLocationMetadata(IPAddress ipAddress) {
        var result = new IPLocationMetadata();
        if (_cityDatabaseReader.TryCity(ipAddress, out var cityResponse)) {
            var latitude = cityResponse?.Location?.Latitude;
            var longitude = cityResponse?.Location?.Longitude;
            if (latitude.HasValue && longitude.HasValue) {
                result.Coordinates = new GeoPoint(latitude.Value, longitude.Value);
            }
            result.CityName = cityResponse?.City?.Name;
            result.PostalCode = cityResponse?.Postal?.Code;
            if (cityResponse?.Subdivisions?.Any() == true) {
                result.Subdivisions.AddRange(cityResponse.Subdivisions.Select(subdivision => subdivision.Name));
            }
        }
        if (_countryDatabaseReader.TryCountry(ipAddress, out var countryResponse)) {
            result.CountryName = countryResponse?.Country?.Name;
            result.CountryIsoCode = countryResponse?.Country?.IsoCode;
            result.Continent = countryResponse?.Continent?.Name;
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