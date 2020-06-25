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
        /// Options for the SMS sent when a user updates his phone number.
        /// </summary>
        public UpdatePhoneNumberOptions PhoneNumber { get; set; } = new UpdatePhoneNumberOptions();
        /// <summary>
        /// Options for the email sent when a user updates his email address.
        /// </summary>
        public UpdateEmailOptions Email { get; set; } = new UpdateEmailOptions();
        /// <summary>
        /// If true, it seeds the database with some initial data for users, roles etc. Works only when environment is 'Development'. Default is false.
        /// </summary>
        public bool UseInitialData { get; set; } = false;
        /// <summary>
        /// If true, various events (user or client created etc.) are raised from the API. Default is false.
        /// </summary>
        public bool CanRaiseEvents { get; set; } = false;
        /// <summary>
        /// A list of initial users to be inserted in the database on startup. Works only when environment is 'Development'.
        /// </summary>
        public IEnumerable<User> InitialUsers { get; set; }
        /// <summary>
        /// Disables the cache for all the endpoints in the IdentityServer API. Defaults to false.
        /// </summary>
        public bool DisableCache { get; set; } = false;
    }
}
