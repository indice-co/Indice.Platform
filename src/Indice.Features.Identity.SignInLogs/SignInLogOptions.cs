using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.SignInLogs;

/// <summary>Options for configuring the IdentityServer sign in logs mechanism.</summary>
public class SignInLogOptions
{
    internal static string GEO_LITE2_CITY_FILE_NAME = "GeoLite2-City.mmdb";
    internal static string GEO_LITE2_COUNTRY_FILE_NAME = "GeoLite2-Country.mmdb";
    private string _apiPrefix;

    internal SignInLogOptions(
        IServiceCollection services, 
        IConfiguration configuration
    ) {
        Services = services;
        Configuration = configuration;
    }

    internal IServiceCollection Services { get; }
    internal IConfiguration Configuration { get; }
    /// <summary>Determines whether personal data (i.e. IP Address) are anonymized when persisted in the database. Defaults to <i>false</i>.</summary>
    public bool AnonymizePersonalData { get; set; }
    /// <summary>API default resource scope. Defaults to <i>identity</i>.</summary>
    public string ApiScope { get; set; } = "identity";
    /// <summary>Database schema for the database. Defaults to <i>dbo</i>.</summary>
    public string DatabaseSchema { get; set; } = "dbo";
    /// <summary>Disable logging sign-in events. Defaults to <i>false</i>.</summary>
    public bool Disable { get; set; }

    /// <summary>Specifies a prefix for the API endpoints.</summary>
    public PathString ApiPrefix {
        get => _apiPrefix;
        set { _apiPrefix = string.IsNullOrWhiteSpace(value) ? string.Empty : value; }
    }

    /// <summary>Creates a deep copy of the options.</summary>
    public SignInLogOptions Clone() => (SignInLogOptions)MemberwiseClone();
}
