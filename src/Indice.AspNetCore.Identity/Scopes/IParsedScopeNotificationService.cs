using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Validation;

namespace Indice.AspNetCore.Identity.Scopes
{
    /// <summary>
    /// Contains methods to notify an API when a dynamic consent is altered.
    /// </summary>
    public interface IParsedScopeNotificationService
    {
        /// <summary>
        /// Abstracts the way that an API is notified.
        /// </summary>
        /// <param name="clientId">The id of the client.</param>
        /// <param name="parsedScopes">The scopes that were altered.</param>
        /// <param name="notificationType">Describes the way that a dynamic consent was altered.</param>
        Task Notify(string clientId, IEnumerable<ParsedScopeValue> parsedScopes, ParsedScopeNotificationType notificationType);
    }

    /// <summary>
    /// Describes the way that a dynamic consent was modified.
    /// </summary>
    public enum ParsedScopeNotificationType
    {
        /// <summary>
        /// Grants was revoked.
        /// </summary>
        GrantsRevoked,
        /// <summary>
        /// Consent was granted.
        /// </summary>
        ConsentGranted
    }
}
