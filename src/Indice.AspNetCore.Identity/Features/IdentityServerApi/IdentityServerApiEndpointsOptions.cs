using System;
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
        /// Use true it seeds the database with some initial data for users, roles etc. Works only on Debug mode. Default is false.
        /// </summary>
        public bool UseInitialData { get; set; } = false;
    }
}
