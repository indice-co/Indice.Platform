using static IdentityServer4.IdentityServerConstants;

namespace Indice.AspNetCore.Identity.Api.Models
{
    /// <summary>
    /// Contains filter when querying for user consents.
    /// </summary>
    public class UserConsentsListFilter
    {
        /// <summary>
        /// The type of consent to look for.
        /// </summary>
        public UserConsentType? ConsentType { get; set; }
        /// <summary>
        /// The id of the client.
        /// </summary>
        public string ClientId { get; set; }
    }

    /// <summary>
    /// The type of user consent.
    /// </summary>
    public enum UserConsentType
    {
        /// <summary>
        /// Authorization Code
        /// </summary>
        AuthorizationCode,
        /// <summary>
        /// Reference Token
        /// </summary>
        ReferenceToken,
        /// <summary>
        /// Refresh Token
        /// </summary>
        RefreshToken,
        /// <summary>
        /// User Consent
        /// </summary>
        UserConsent,
        /// <summary>
        /// Device Code
        /// </summary>
        DeviceCode,
        /// <summary>
        /// User Code
        /// </summary>
        UserCode
    }

    /// <summary>
    /// Extension methods on <see cref="UserConsentType"/> enum.
    /// </summary>
    public static class UserConsentTypeExtensions
    {
        /// <summary>
        /// Transforms <see cref="UserConsentType"/> enum to it's string value.
        /// </summary>
        /// <param name="consentType">The <see cref="UserConsentType"/> enum.</param>
        public static string ToConstantName(this UserConsentType? consentType) => consentType switch {
            UserConsentType.AuthorizationCode => PersistedGrantTypes.AuthorizationCode,
            UserConsentType.ReferenceToken => PersistedGrantTypes.ReferenceToken,
            UserConsentType.RefreshToken => PersistedGrantTypes.RefreshToken,
            UserConsentType.UserConsent => PersistedGrantTypes.UserConsent,
            UserConsentType.DeviceCode => PersistedGrantTypes.DeviceCode,
            UserConsentType.UserCode => PersistedGrantTypes.UserCode,
            _ => default,
        };
    }
}
