namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary></summary>
public class UiFeaturesInfo
{
    /// <summary>Determines whether dashboard metrics should be visible.</summary>
    public bool MetricsEnabled { get; set; }
    /// <summary>Determines whether sign in logs should be visible.</summary>
    public bool SignInLogsEnabled { get; set; }
    /// <summary>Gets a flag indicating whether the backing user store supports user name that are the same as emails.</summary>
    /// <remarks>When set to true the email and username columns can usualy be merged into one field in the ui. A good case would be data table columns</remarks>
    public bool EmailAsUserName { get; set; }
}
