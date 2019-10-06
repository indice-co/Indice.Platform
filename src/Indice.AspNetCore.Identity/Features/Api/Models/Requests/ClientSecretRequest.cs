using System;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models a client secret that will be created on the server.
    /// </summary>
    public class ClientSecretRequest
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
        /// <summary>
        /// The type of client secret.
        /// </summary>
        public ClientSecretType Type { get; set; }
    }

    /// <summary>
    /// The type of client secret.
    /// </summary>
    public enum ClientSecretType
    {
        /// <summary>
        /// X509 Thumbprint.
        /// </summary>
        X509Thumbprint,
        /// <summary>
        /// Shared Secret.
        /// </summary>
        SharedSecret
    }
}
