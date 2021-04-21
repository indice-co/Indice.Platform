namespace Indice.AspNetCore.Identity.Api.Models
{
    /// <summary>
    /// Models a claim.
    /// </summary>
    public class ClaimInfo : BasicClaimInfo
    {
        /// <summary>
        /// The id of the user claim entry.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// The display name of the claim.
        /// </summary>
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// Models a claim.
    /// </summary>
    public class BasicClaimInfo
    {
        /// <summary>
        /// The type of the claim.
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// The value of the claim.
        /// </summary>
        public string Value { get; set; }
    }
}
