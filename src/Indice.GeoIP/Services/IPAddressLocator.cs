using System.Net;
using Indice.GeoIP.Extensions;
using Indice.GeoIP.GeoLite2;
using Indice.GeoIP.Models;
using Indice.Types;

namespace Indice.GeoIP.Services;

/// <summary>
/// Service responsible for resolving geolocation metadata given an IP address.
/// </summary>
public sealed class IPAddressLocator : IDisposable
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
        var result = new IPLocationMetadata() {
            IPAddress = ipAddress.ToString(),
        };
        if (IPAddress.IsLoopback(ipAddress) || ipAddress.IsPrivate()) {
            return result;
        }
        if (_cityDatabaseReader.TryCity(ipAddress, out var cityResponse)) {
            var latitude = cityResponse?.Location?.Latitude;
            var longitude = cityResponse?.Location?.Longitude;
            if (latitude.HasValue && longitude.HasValue) {
                result.Coordinates = new GeoPoint(latitude.Value, longitude.Value);
            }
            result.CityName = cityResponse?.City?.Name;
            result.PostalCode = cityResponse?.Postal?.Code;
            if (cityResponse?.Subdivisions?.Any() == true) {
                result.Subdivisions.AddRange(cityResponse.Subdivisions.Select(subdivision => subdivision.Name!));
            }
        }
        if (_countryDatabaseReader.TryCountry(ipAddress, out var countryResponse)) {
            result.CountryName = countryResponse?.Country?.Name;
            result.CountryIsoCode = countryResponse?.Country?.IsoCode;
            result.Continent = countryResponse?.Continent?.Name;
        }
        return result;
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>"
    public void Dispose() {
        _cityDatabaseReader?.Dispose();
        _countryDatabaseReader?.Dispose();
    }
}
