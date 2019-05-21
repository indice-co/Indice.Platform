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
    }
}
