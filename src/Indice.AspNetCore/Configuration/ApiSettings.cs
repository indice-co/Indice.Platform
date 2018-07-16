using System.Collections.Generic;

namespace Indice.Configuration
{
    /// <summary>
    /// Settings related to APIs.
    /// </summary>
    public class ApiSettings
    {
        /// <summary>
        /// Security wise the name of the api. Used for identifyinh the api as a resource in oAuth2 as well as the main scope.
        /// </summary>
        public string ResourceName { get; set; } = "api1";

        /// <summary>
        /// The sub scopes avialable for the api.
        /// </summary>
        public Dictionary<string, string> Scopes { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Friendly name for the api
        /// </summary>
        public string FriendlyName { get; set; } = "My Api Name";

        /// <summary>
        /// The api verison number.
        /// </summary>
        public string DefaultVersion { get; set; } = "1";
    }
}
