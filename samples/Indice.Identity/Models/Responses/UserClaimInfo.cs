namespace Indice.Identity.Models
{
    /// <summary>
    /// Models a user's claim.
    /// </summary>
    public class UserClaimInfo : BasicUserClaimInfo
    {
        /// <summary>
        /// The id of the user claim entry.
        /// </summary>
        public int Id { get; set; }
    }

    /// <summary>
    /// Models a user's claim.
    /// </summary>
    public class BasicUserClaimInfo
    {
        /// <summary>
        /// The type of the claim.
        /// </summary>
        public string ClaimType { get; set; }
        /// <summary>
        /// The value of the claim.
        /// </summary>
        public string ClaimValue { get; set; }
    }
}
