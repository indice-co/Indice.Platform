using System.Collections.Generic;
using Indice.AspNetCore.Identity.Models;
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
        /// If true, it seeds the database with some initial data for users, roles etc. Works only when environment is 'Development'. Default is false.
        /// </summary>
        public bool UseInitialData { get; set; } = false;
        /// <summary>
        /// If true, various events (user or client created etc.) are raised from the API. Default is false.
        /// </summary>
        public bool RaiseEvents { get; set; } = false;
        /// <summary>
        /// A list of initial users to be inserted in the database on startup. Works only when environment is 'Development'.
        /// </summary>
        public IEnumerable<User> InitialUsers { get; set; }
    }
}
