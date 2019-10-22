using System.Collections.Generic;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models a resource (identity or API) that will be created on the server.
    /// </summary>
    public class CreateResourceRequest
    {
        /// <summary>
        /// The unique name of the resource.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Display name of the resource.
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Description of the resource.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// List of accociated user claims that should be included when this resource is requested.
        /// </summary>
        public List<string> UserClaims { get; set; }
    }
}
