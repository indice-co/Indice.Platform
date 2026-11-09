using MaxMind.GeoIP2;

namespace Indice.Features.GeoIP.GeoLite2;

/// <summary>
/// A reader implementation for the GeoLite2 Country database.
/// </summary>
public sealed class CountryDatabaseReader : DatabaseReader
{
    /// <summary>
    /// Initializes a new instance of the CountryDatabaseReader class using the embedded GeoLite2 Country database file.
    /// </summary>
    public CountryDatabaseReader() :        
        base(typeof(CountryDatabaseReader).Assembly.GetManifestResourceStream($"Indice.Features.GeoIP.GeoLite2.{Constants.GEO_LITE2_COUNTRY_FILE_NAME}")
             ?? throw new InvalidOperationException($"Embedded GeoLite2 Country database resource '{Constants.GEO_LITE2_COUNTRY_FILE_NAME}' was not found.")) { }
}