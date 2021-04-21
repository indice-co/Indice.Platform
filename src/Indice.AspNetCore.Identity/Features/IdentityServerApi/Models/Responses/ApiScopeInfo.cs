using System.Collections.Generic;
using Indice.Types;

namespace Indice.AspNetCore.Identity.Api.Models
{
    /// <summary>
    /// Models access to an API resource.
    /// </summary>
    public class ApiScopeInfo
    {
        /// <summary>
        /// Unique identifier for the scope.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// The name of the scope.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The display name of the scope.
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// The description of the resource.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Determines whether this scope should be displayed emphasized or not.
        /// </summary>
        public bool? Emphasize { get; set; }
        /// <summary>
        /// Determines whether this scope should be displayed in the discovery document or not.
        /// </summary>
        public bool? ShowInDiscoveryDocument { get; set; }
        /// <summary>
        /// Translations.
        /// </summary>
        public TranslationDictionary<ApiScopeTranslation> Translations { get; set; }
        /// <summary>
        /// List of accociated user claims that should be included when a resource is requested.
        /// </summary>
        public IEnumerable<string> UserClaims { get; set; }
    }

    /// <summary>
    /// Translation object for type <see cref="ApiScopeInfo"/>.
    /// </summary>
    public class ApiScopeTranslation
    {
        /// <summary>
        /// The display name of the scope.
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// The description of the resource.
        /// </summary>
        public string Description { get; set; }
    }
}
