using MaxMind.GeoIP2;

namespace Indice.Features.Identity.SignInLogs.GeoLite2;

/// <summary></summary>
public sealed class CityDatabaseReader : DatabaseReader
{
    /// <summary></summary>
    public CityDatabaseReader() : 
        base(typeof(CityDatabaseReader).Assembly.GetManifestResourceStream($"Indice.Features.Identity.SignInLogs.GeoLite2.{SignInLogOptions.GEO_LITE2_CITY_FILE_NAME}")) { }
}