using System.Text;
using Indice.Types;

namespace Indice.Features.Identity.Core;
/// <summary>Models the geolocation data that were retrieved by the incoming IP address.</summary>
public class IPLocationMetadata
{
    /// <summary>
    /// The IP address that was used to retrieve the geolocation data.
    /// </summary>
    public string IPAddress { get; set; } = null!;
    /// <summary>The city name.</summary>
    public string? CityName { get; set; }
    /// <summary>Subdivisions.</summary>
    public List<string> Subdivisions { get; set; } = [];
    /// <summary>The country name.</summary>
    public string? CountryName { get; set; }
    /// <summary>The country two letter ISO code.</summary>
    public string? CountryIsoCode { get; set; }
    /// <summary>The postal code.</summary>
    public string? PostalCode { get; set; }
    /// <summary>The continent name.</summary>
    public string? Continent { get; set; }
    /// <summary></summary>
    public GeoPoint? Coordinates { get; set; }

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
