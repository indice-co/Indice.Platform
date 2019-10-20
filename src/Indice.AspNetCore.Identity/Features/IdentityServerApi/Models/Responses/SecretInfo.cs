using System;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models a secret value used for a client or API.
    /// </summary>
    public class SecretInfo
    {
        /// <summary>
        /// The identifier for the API secret.
        /// </summary>
        public int Id { get; set; }
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
        public SecretType Type { get; set; }
    }

    /// <summary>
    /// Models an API secret used for the introspection endpoint. The API can authenticate with introspection using the API name and secret.
    /// </summary>
    public class ApiSecretInfo : SecretInfo { }

    /// <summary>
    /// Models an Client secret used in flows that require this.
    /// </summary>
    public class ClientSecretInfo : SecretInfo { }
}
