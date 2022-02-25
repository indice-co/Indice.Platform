namespace Indice.AspNetCore.Identity.Api.Configuration
{
    /// <summary>
    /// Feature flags for Identity Server API.
    /// </summary>
    public class IdentityServerApiFeatures
    {
        /// <summary>
        /// Enables API for public registration API.
        /// </summary>
        public const string PublicRegistration = nameof(PublicRegistration);
        /// <summary>
        /// Enables API for public registration API.
        /// </summary>
        public const string DashboardMetrics = nameof(DashboardMetrics);
    }
}
