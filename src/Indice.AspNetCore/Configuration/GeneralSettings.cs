using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.Configuration
{
    /// <summary>
    /// General settings for an ASP net core app
    /// </summary>
    public class GeneralSettings
    {
        /// <summary>
        /// The name is used to mark the section found inside a configuration file.
        /// </summary>
        public static readonly string Name = "General";

        /// <summary>
        /// Url that the app is Hosed under
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The url for the IdentityServer.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// The name of the app. Usualy used for the Layout page Title inside an Html header. 
        /// </summary>
        public string ApplicationName { get; set; } = "My App name";

        /// <summary>
        /// A descritpion for the app.
        /// </summary>
        public string ApplicationDescription { get; set; } = "My App does this and that.";

        /// <summary>
        /// Client credentials for machine to machine configuration.
        /// </summary>
        public ClientSettings Client { get; set; }

        /// <summary>
        /// Api settings if Api is present.
        /// </summary>
        public ApiSettings Api { get; set; }

        /// <summary>
        /// Swagger endpoint toggle
        /// </summary>
        public bool EnableSwagger { get; set; }
    }
}
