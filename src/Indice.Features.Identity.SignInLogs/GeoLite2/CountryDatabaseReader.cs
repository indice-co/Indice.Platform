using MaxMind.GeoIP2;

namespace Indice.Features.Identity.SignInLogs.GeoLite2;

/// <summary></summary>
public sealed class CountryDatabaseReader : DatabaseReader
{
    /// <summary></summary>
    public CountryDatabaseReader() :
        base(typeof(CountryDatabaseReader).Assembly.GetManifestResourceStream($"Indice.Features.Identity.SignInLogs.GeoLite2.{SignInLogOptions.GEO_LITE2_COUNTRY_FILE_NAME}")) { }
}