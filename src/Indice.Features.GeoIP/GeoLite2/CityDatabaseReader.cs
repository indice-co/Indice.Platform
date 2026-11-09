using MaxMind.GeoIP2;

namespace Indice.Features.GeoIP.GeoLite2;

/// <summary>
/// A reader implementation for the GeoLite2 City database. 
/// Reading the GeoLite2 binary file format as embedded resource.
/// </summary>
public sealed class CityDatabaseReader : DatabaseReader
{
    /// <summary>
    /// Initializes a new instance of the CityDatabaseReader class using the embedded GeoLite2 City database file.
    /// </summary>
    public CityDatabaseReader() :
        base(typeof(CityDatabaseReader).Assembly.GetManifestResourceStream($"Indice.Features.GeoIP.GeoLite2.{Constants.GEO_LITE2_CITY_FILE_NAME}")
             ?? throw new InvalidOperationException($"Embedded GeoLite2 City database resource '{Constants.GEO_LITE2_CITY_FILE_NAME}' was not found.")) { }
}