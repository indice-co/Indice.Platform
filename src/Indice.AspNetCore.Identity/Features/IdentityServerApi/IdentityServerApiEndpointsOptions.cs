using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Options for configuring the IdentityServer API feature.
    /// </summary>
    public class IdentityServerApiEndpointsOptions
    {
        internal IServiceCollection Services;
        /// <summary>
        /// If true, it seeds the database with some initial data for users, roles etc. Works only on Debug mode. Default is false.
        /// </summary>
        public bool UseInitialData { get; set; } = false;
        /// <summary>
        /// If true, various events (user or client created etc.) are raised from the API. Default is false.
        /// </summary>
        public bool RaiseEvents { get; set; } = false;
    }
}
