namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary></summary>
public class UiFeaturesInfo
{
    /// <summary>Determines whether dashboard metrics should be visible.</summary>
    public bool MetricsEnabled { get; set; }
    /// <summary>Determines whether sign in logs should be visible.</summary>
    public bool SignInLogsEnabled { get; set; }
}
