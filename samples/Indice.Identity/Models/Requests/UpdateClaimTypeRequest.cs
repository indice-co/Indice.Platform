using Indice.Identity.Data.Models;

namespace Indice.Identity.Models
{
    /// <summary>
    /// Models a claim type that will be updated on the server.
    /// </summary>
    public class UpdateClaimTypeRequest
    {
        /// <summary>
        /// The name used for display purposes.
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// A description.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Determines whether this claim is required to create new users.
        /// </summary>
        public bool Required { get; set; }
        /// <summary>
        /// Determines whether this claim will be editable by a user if exposed through a public API.
        /// </summary>
        public bool UserEditable { get; set; }
        /// <summary>
        /// A regex rule that constraints the values of the claim.
        /// </summary>
        public string Rule { get; set; }
        /// <summary>
        /// The value type of the claim.
        /// </summary>
        public ValueType ValueType { get; set; }
    }
}
