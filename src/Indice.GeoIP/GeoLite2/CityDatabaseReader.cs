using MaxMind.GeoIP2;

namespace Indice.GeoIP.GeoLite2;

/// <summary>
/// A reader implementation for the GeoLite2 City database. 
/// Reading the GeoLite2 binary file format as embeded resource.
/// </summary>
public sealed class CityDatabaseReader : DatabaseReader
{
    /// <summary>
    /// Initializes a new instance of the CityDatabaseReader class using the embedded GeoLite2 City database file.
    /// </summary>
    public CityDatabaseReader() :
        base(typeof(CityDatabaseReader).Assembly.GetManifestResourceStream($"Indice.GeoIP.GeoLite2.{Constants.GEO_LITE2_CITY_FILE_NAME}")!) { }
}