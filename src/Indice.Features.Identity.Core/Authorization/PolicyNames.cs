namespace Indice.Features.Identity.Core.Authorization
{
    /// <summary>Basic set of authorization policy names.</summary>
    public class BasicPolicyNames
    {
        /// <summary>Only a user marked as administrator in the user store or with a role assignment of the name 'Administrator' is allowed.</summary>
        public const string BeAdmin = nameof(BeAdmin);
    }
}
