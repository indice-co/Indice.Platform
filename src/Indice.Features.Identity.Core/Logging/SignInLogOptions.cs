using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.Core.Logging;

/// <summary>Options for configuring the IdentityServer sign in logs mechanism.</summary>
public class SignInLogOptions
{
    internal static string GEO_LITE2_CITY_FILE_NAME = "GeoLite2-City.mmdb";
    internal static string GEO_LITE2_COUNTRY_FILE_NAME = "GeoLite2-Country.mmdb";
    internal IServiceCollection Services;
}
