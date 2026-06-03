using MaxMind.GeoIP2;

namespace Indice.Features.GeoIP.GeoLite2;

/// <summary>
/// A reader implementation for the GeoLite2 ASN database. 
/// Reading the GeoLite2 binary file format as embeded resource.
/// </summary>
public sealed class AsnDatabaseReader : DatabaseReader
{
    /// <summary>
    /// Initializes a new instance of the AsnDatabaseReader class using the embedded GeoLite2 ASN database file.
    /// </summary>
    public AsnDatabaseReader() :
        base(typeof(AsnDatabaseReader).Assembly.GetManifestResourceStream($"Indice.Features.GeoIP.GeoLite2.{Constants.GEO_LITE2_ASN_FILE_NAME}")
             ?? throw new InvalidOperationException($"Embedded GeoLite2 ASN database resource '{Constants.GEO_LITE2_ASN_FILE_NAME}' was not found.")) { }
}