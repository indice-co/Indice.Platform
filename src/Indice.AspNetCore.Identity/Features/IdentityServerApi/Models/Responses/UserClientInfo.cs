using System;
using System.Collections.Generic;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models a system client that a user has given consent to or currently has IdentityServer side tokens for.
    /// </summary>
    public class UserClientInfo : ClientInfo
    {
        /// <summary>
        /// Creation of grant.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>
        /// Expiration of grant.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
        /// <summary>
        /// Resources/scopes accessible by the application.
        /// </summary>
        public IEnumerable<string> Scopes { get; set; }
    }
}
