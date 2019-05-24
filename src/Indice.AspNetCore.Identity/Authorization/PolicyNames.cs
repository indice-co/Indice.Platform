namespace Indice.AspNetCore.Identity.Authorization
{
    /// <summary>
    /// Basic set of authorization policy names.
    /// </summary>
    public class BasicPolicyNames
    {
        /// <summary>
        /// Only a user marked as Admin in the user store or with a role assignment of the name 'Administrator' is allowed.
        /// </summary>
        public const string BeAdmin = nameof(BeAdmin);
    }
}
