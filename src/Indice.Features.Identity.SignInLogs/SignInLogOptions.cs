using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.SignInLogs;

/// <summary>Options for configuring the IdentityServer sign in logs mechanism.</summary>
public class SignInLogOptions
{
    internal static string GEO_LITE2_CITY_FILE_NAME = "GeoLite2-City.mmdb";
    internal static string GEO_LITE2_COUNTRY_FILE_NAME = "GeoLite2-Country.mmdb";

    internal SignInLogOptions(IServiceCollection services, IConfiguration configuration) {
        Services = services;
        Configuration = configuration;
    }

    internal IServiceCollection Services { get; }
    internal IConfiguration Configuration { get; }
}
