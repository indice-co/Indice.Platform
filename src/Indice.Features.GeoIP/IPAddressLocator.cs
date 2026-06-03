using System.Net;
using Indice.Features.GeoIP.Extensions;
using Indice.Features.GeoIP.GeoLite2;
using Indice.Types;

namespace Indice.Features.GeoIP;

/// <summary>
/// Service responsible for resolving geolocation metadata given an IP address.
/// </summary>
public sealed class IPAddressLocator
{
    private readonly CityDatabaseReader _cityDatabaseReader;
    private readonly CountryDatabaseReader _countryDatabaseReader;
    private readonly AsnDatabaseReader _asnDatabaseReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="IPAddressLocator"/> class.
    /// </summary>
    /// <param name="cityDatabaseReader">The city database reader.</param>
    /// <param name="countryDatabaseReader">The country database reader.</param>
    /// <param name="asnDatabaseReader">The ASN database reader.</param>
    public IPAddressLocator(CityDatabaseReader cityDatabaseReader, CountryDatabaseReader countryDatabaseReader, AsnDatabaseReader asnDatabaseReader) {
        _cityDatabaseReader = cityDatabaseReader ?? throw new ArgumentNullException(nameof(cityDatabaseReader));
        _countryDatabaseReader = countryDatabaseReader ?? throw new ArgumentNullException(nameof(countryDatabaseReader));
        _asnDatabaseReader = asnDatabaseReader ?? throw new ArgumentNullException(nameof(asnDatabaseReader));
    }

    /// <summary>Gets various geolocation data for the given <see cref="IPAddress"/>.</summary>
    /// <param name="ipAddress">The IP address to look for.</param>
    public IPAddressLocation GetLocationMetadata(IPAddress ipAddress) {
        var result = new IPAddressLocation() {
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
        if (_asnDatabaseReader.TryAsn(ipAddress, out var asnResponse)) {
            result.ASN = asnResponse?.AutonomousSystemNumber;
            result.ASOrganization = asnResponse?.AutonomousSystemOrganization;
        }
        return result;
    }
}
