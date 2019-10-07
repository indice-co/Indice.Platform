namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models a user's claim.
    /// </summary>
    public class CreateUserClaimRequest
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
