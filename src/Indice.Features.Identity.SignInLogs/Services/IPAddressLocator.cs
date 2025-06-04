using System.Net;
using System.Text;
using Indice.Features.Identity.Core;
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
        var result = new IPLocationMetadata() { 
            IPAddress = ipAddress.ToString(),
        };
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
}

