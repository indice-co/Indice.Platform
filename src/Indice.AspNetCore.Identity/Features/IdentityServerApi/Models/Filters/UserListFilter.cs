namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Contains filter when querying for users list.
    /// </summary>
    public class UserListFilter
    {
        /// <summary>
        /// The claim type and value to search for.
        /// </summary>
        public BasicClaimInfo Claim { get; set; }
    }
}
