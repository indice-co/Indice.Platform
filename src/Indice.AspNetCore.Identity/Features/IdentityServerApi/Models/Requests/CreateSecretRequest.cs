using System;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models a client or API secret that will be created on the server.
    /// </summary>
    public class CreateSecretRequest
    {
        /// <summary>
        /// Description of client secret.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The value of client secret.
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// Optional expiration of client secret.
        /// </summary>
        public DateTime? Expiration { get; set; }
    }
}
