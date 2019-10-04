using System.Collections.Generic;

namespace Indice.Identity.Models
{
    /// <summary>
    /// Models an API resource for the application.
    /// </summary>
    public class ApiResourceInfo
    {
        /// <summary>
        /// Unique identifier for the API resource.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// The name of the resource.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The display name of the resource.
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// The description of the resource.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Determines whether this resource is enabled or not.
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// Determines whether this resource is editable or not.
        /// </summary>
        public bool NonEditable { get; set; }
        /// <summary>
        /// List of accociated claims that should be included when this resource is requested.
        /// </summary>
        public IEnumerable<string> AllowedClaims { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ScopeInfo> Scopes { get; set; }
    }
}
