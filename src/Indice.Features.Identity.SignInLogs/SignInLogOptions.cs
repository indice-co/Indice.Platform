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
    /// <summary>Determines whether personal data (i.e. IP Address) are anonymized when persisted in the database. Defaults to <i>false</i>.</summary>
    public bool AnonymizePersonalData { get; set; }
    /// <summary>API default resource scope. Defaults to <i>identity</i>.</summary>
    public string ApiScope { get; set; } = "identity";
    /// <summary>Cleanup options.</summary>
    public LogCleanupOptions Cleanup { get; set; } = new LogCleanupOptions();
    /// <summary>Schema name to be used for the database, in case a relational provider is configured. Defaults to <i>dbo</i>.</summary>
    public string DatabaseSchema { get; set; } = "dbo";
    /// <summary>Determines whether logging sign-in events is enabled. Defaults to <i>true</i>.</summary>
    public bool Enable { get; set; } = true;

    /// <summary>Specifies a prefix for the API endpoints.</summary>
    public PathString ApiPrefix {
        get => _apiPrefix;
        set { _apiPrefix = string.IsNullOrWhiteSpace(value) ? string.Empty : value; }
    }

    /// <summary>Performs a deep copy of the current <see cref="SignInLogOptions"/> instance.</summary>
    public SignInLogOptions Copy() => new() {
        AnonymizePersonalData = AnonymizePersonalData,
        ApiPrefix = ApiPrefix,
        ApiScope = ApiScope,
        Cleanup = new LogCleanupOptions {
            BatchSize = Cleanup.BatchSize,
            IntervalSeconds = Cleanup.IntervalSeconds,
            Enable = Cleanup.Enable,
            RetentionDays = Cleanup.RetentionDays,
        },
        DatabaseSchema = DatabaseSchema,
        Enable = Enable
    };
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
