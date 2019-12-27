namespace Indice.AspNetCore.Identity.Authorization
{
    /// <summary>
    /// Represents extended options you can use to configure the cookies middleware used by the identity system.
    /// </summary>
    public class ExtendedIdentityConstants
    {
        private const string CookiePrefix = "Identity";
        /// <summary>
        /// The scheme used to identify extended validation authentication cookies for round tripping user identities.
        /// </summary>
        public const string ExtendedValidationUserIdScheme = CookiePrefix + ".ExtendedValidationUserId";
        /// <summary>
        /// A claim type used to store temp data regarding password exporation that will be used in a partial login 
        /// to identify that a user is in need for an immediate password change
        /// </summary>
        public const string PasswordExpiredClaimType = "PasswordExpired";
    }
}
