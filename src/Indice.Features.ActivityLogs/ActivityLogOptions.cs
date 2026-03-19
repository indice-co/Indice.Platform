using Microsoft.AspNetCore.Http;

namespace Indice.Features.ActivityLogs;

/// <summary>Options for configuring the IdentityServer activity logs mechanism.</summary>
public class ActivityLogOptions
{
    private string _apiPrefix = "/api";

    /// <summary>Creates a new instance of <see cref="ActivityLogOptions"/> class.</summary>
    public ActivityLogOptions() { }

    /// <summary>Determines which of the default enrichers will not be used </summary>
    public List<Type> ExcludedEnrichers { get; } = [];
    /// <summary>Determines whether personal data (i.e. IP Address) are anonymized when persisted in the database. Defaults to <i>false</i>.</summary>
    public bool AnonymizePersonalData { get; set; }
    /// <summary>Cleanup options.</summary>
    public LogCleanupOptions Cleanup { get; set; } = new LogCleanupOptions();
    /// <summary>Schema name to be used for the database, in case a relational provider is configured. Defaults to <i>app</i>.</summary>
    public string DatabaseSchema { get; set; } = "app";
    /// <summary>Determines whether activity logging is enabled. Defaults to <i>true</i>.</summary>
    public bool Enable { get; set; } = true;
    /// <summary>The maximum number of items the internal queue may store. Defaults to <i>100</i>.</summary>
    public int QueueChannelCapacity { get; set; } = 100;
    /// <summary>Specifies a prefix for the API endpoints. Defaults to <i>/api</i>.</summary>
    public PathString ApiPrefix {
        get => _apiPrefix;
        set { _apiPrefix = string.IsNullOrWhiteSpace(value) ? string.Empty : value; }
    }
    /// <summary>The number of items the dequeue batch contains. Should be extra careful when configuring!!!</summary>
    public int DequeueBatchSize { get; set; } = 10;
    /// <summary>The timeout milliseconds the queue waits to reach the batch size. Should be extra careful when configuring!!!</summary>
    public long DequeueTimeoutInMilliseconds { get; set; } = 1000;
    /// <summary>The set of categories for activity logs.</summary>
    public HashSet<string> Categories { get; } = new(StringComparer.OrdinalIgnoreCase) {
        "Authentication",
        "Authorization",
        "DataAccess",
        "UserManagement",
        "Security",
        "System",
        "DataModification",
        "BusinessProcess"
    };
}

/// <summary>Options regarding log cleanup.</summary>
public class LogCleanupOptions
{
    /// <summary>The number of log items to delete on each cleanup iteration. Defaults to <i>1000</i>. Maximum allowed values before lock escalation.</summary>
    public ushort BatchSize { get; set; } = 4000;
    /// <summary>The number of seconds to wait between to consecutive cleanup executions. Defaults to <i>3600 seconds</i> (1 hour).</summary>
    public ushort IntervalSeconds { get; set; } = 3600;
    /// <summary>Determines whether log cleanup is enabled. Defaults to <i>false</i>.</summary>
    public bool Enable { get; set; } = false;
    /// <summary>The number of days to maintain a log entry. Defaults to <i>90 days</i>.</summary>
    public ushort RetentionDays { get; set; } = 90;
}