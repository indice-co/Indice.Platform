using System;
using System.Collections.Generic;

namespace Indice.AspNetCore.Identity.Api.Models
{
    /// <summary>
    /// Models the 
    /// </summary>
    public class UserConsentInfo
    {
        /// <summary>
        /// The client id.
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// Consent creation <see cref="DateTime"/>.
        /// </summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>
        /// Consent expiration <see cref="DateTime"/>.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
        /// <summary>
        /// Consent type.
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Associated scopes.
        /// </summary>
        public IEnumerable<string> Scopes { get; set; } = new List<string>();
        /// <summary>
        /// Associated claims.
        /// </summary>
        public IEnumerable<BasicClaimInfo> Claims { get; set; } = new List<BasicClaimInfo>();
    }
}
