using Newtonsoft.Json;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models an application claim type.
    /// </summary>
    public class ClaimTypeInfo
    {
        /// <summary>
        /// The unique id of the claim.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The name.
        /// </summary>
        public string Name { get; set; }
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
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public bool Required { get; set; }
        /// <summary>
        /// Determines whether this is a system reserved claim.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public bool Reserved { get; set; }
        /// <summary>
        /// Determines whether this claim will be editable by a user if exposed through a public API.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public bool UserEditable { get; set; }
        /// <summary>
        /// A regex rule that constraints the values of the claim.
        /// </summary>
        public string Rule { get; set; }
        /// <summary>
        /// The value type of the claim.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public ValueType ValueType { get; set; }
    }
}
