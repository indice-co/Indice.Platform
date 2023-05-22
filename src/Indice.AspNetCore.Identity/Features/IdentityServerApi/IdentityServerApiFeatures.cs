namespace Indice.AspNetCore.Identity.Api.Configuration;

/// <summary>Feature flags for Identity Server API.</summary>
public class IdentityServerApiFeatures
{
    /// <summary>Enables API for public registration API.</summary>
    public const string PublicRegistration = nameof(PublicRegistration);
    /// <summary>Enables API for Dashboard Metrics API.</summary>
    public const string DashboardMetrics = nameof(DashboardMetrics);
    /// <summary>Enables API for SetBlock API endpoint.</summary>
    public const string SetBlock = nameof(SetBlock);
}
