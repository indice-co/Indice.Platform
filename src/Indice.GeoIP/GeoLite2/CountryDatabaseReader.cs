using MaxMind.GeoIP2;

namespace Indice.GeoIP.GeoLite2;

/// <summary>
/// A reader implementation for the GeoLite2 Country database.
/// </summary>
public sealed class CountryDatabaseReader : DatabaseReader
{
    /// <summary></summary>
    public CountryDatabaseReader() :
        base(typeof(CountryDatabaseReader).Assembly.GetManifestResourceStream($"Indice.GeoResolve.GeoLite2.{Constants.GEO_LITE2_COUNTRY_FILE_NAME}")!) { }
}