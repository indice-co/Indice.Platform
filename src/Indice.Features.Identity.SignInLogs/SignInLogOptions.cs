using Indice.Features.Identity.Core.ImpossibleTravel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.SignInLogs;

/// <summary>Options for configuring the IdentityServer sign in logs mechanism.</summary>
public class SignInLogOptions
{
    /// <summary>Default value for <see cref="Enable"/> property.</summary>
    public const bool DEFAULT_ENABLE = true;
    /// <summary>Default value for <see cref="ImpossibleTravelGuard"/> property.</summary>
    public const bool DEFAULT_IMPOSSIBLE_TRAVEL_GUARD = false;
    internal static string GEO_LITE2_CITY_FILE_NAME = "GeoLite2-City.mmdb";
    internal static string GEO_LITE2_COUNTRY_FILE_NAME = "GeoLite2-Country.mmdb";
    private string _apiPrefix = "/api";

    /// <summary>Creates a new instance of <see cref="SignInLogOptions"/> class.</summary>
    public SignInLogOptions() { }

    internal SignInLogOptions(
        IServiceCollection services,
        IConfiguration configuration
    ) {
        Services = services;
        Configuration = configuration;
    }

    internal IServiceCollection Services { get; }
    internal IConfiguration Configuration { get; }
    internal List<Type> ExcludedEnrichers { get; } = new List<Type>();
    /// <summary>Determines whether personal data (i.e. IP Address) are anonymized when persisted in the database. Defaults to <i>false</i>.</summary>
    public bool AnonymizePersonalData { get; set; }
    /// <summary>API default resource scope. Defaults to <i>identity</i>.</summary>
    public string ApiScope { get; set; } = "identity";
    /// <summary>Cleanup options.</summary>
    public LogCleanupOptions Cleanup { get; set; } = new LogCleanupOptions();
    /// <summary>Schema name to be used for the database, in case a relational provider is configured. Defaults to <i>auth</i>.</summary>
    public string DatabaseSchema { get; set; } = "auth";
    /// <summary>Determines whether logging sign-in events is enabled. Defaults to <i>true</i>.</summary>
    /// <remarks>If not set, then the <b>IdentityServer:Features:SignInLogs</b> application setting is used.</remarks>
    public bool Enable { get; set; } = DEFAULT_ENABLE;
    /// <summary>The maximum number of items the internal queue may store. Defaults to <i>100</i>.</summary>
    public int QueueChannelCapacity { get; set; } = 100;
    /// <summary>Determines whether impossible travel detection is enabled. Defaults to <i>false</i>.</summary>
    /// <remarks>
    /// SignInLogs feature must also be enabled for this to take effect.<br />
    /// If not set, then the <b>IdentityServer:Features:ImpossibleTravel</b> application setting is used.
    /// </remarks>
    public bool ImpossibleTravelGuard { get; set; } = DEFAULT_IMPOSSIBLE_TRAVEL_GUARD;
    /// <summary>The speed (km/h) used to compare the travel speed between two login attempts. Default is 80 km/h.</summary>
    public double ImpossibleTravelAcceptableSpeed { get; set; } = 80d;
    /// <summary>Specifies the flow to follow when impossible travel is detected for the current user. Defaults to <see cref="ImpossibleTravelFlowType.PromptMfa"/>.</summary>
    public ImpossibleTravelFlowType ImpossibleTravelFlowType { get; set; } = ImpossibleTravelFlowType.PromptMfa;

    /// <summary>Specifies a prefix for the API endpoints. Defaults to <i>/api</i>.</summary>
    public PathString ApiPrefix {
        get => _apiPrefix;
        set { _apiPrefix = string.IsNullOrWhiteSpace(value) ? string.Empty : value; }
    }
}

/// <summary>Options regarding log cleanup.</summary>
public class LogCleanupOptions
{
    /// <summary>The number of log items to delete on each cleanup iteration. Defaults to <i>250</i>.</summary>
    public ushort BatchSize { get; set; } = 250;
    /// <summary>The number of seconds to wait between to consecutive cleanup executions. Defaults to <i>3600 seconds</i> (1 hour).</summary>
    public ushort IntervalSeconds { get; set; } = 3600;
    /// <summary>Determines whether log cleanup is enabled. Defaults to <i>true</i>.</summary>
    public bool Enable { get; set; } = true;
    /// <summary>The number of days to maintain a log entry. Defaults to <i>90 days</i>.</summary>
    public ushort RetentionDays { get; set; } = 90;
}
