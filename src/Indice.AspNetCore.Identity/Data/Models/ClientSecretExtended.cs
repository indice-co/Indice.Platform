using IdentityServer4.EntityFramework.Entities;
using Newtonsoft.Json;

namespace Indice.AspNetCore.Identity.Data.Models
{
    /// <summary>
    /// Extends the IdentityServer4 <see cref="IdentityServer4.EntityFramework.Entities.ClientSecret"/> table.
    /// </summary>
    public class ClientSecretExtended
    {
        /// <summary>
        /// The id of the client.
        /// </summary>
        public int ClientSecretId { get; set; }
        /// <summary>
        /// Custom data for the client secret entry.
        /// </summary>
        public dynamic CustomData { get; set; }
        /// <summary>
        /// Custom data for the client secret entry, in the form of JSON.
        /// </summary>
        public string CustomDataJson {
            get => CustomData != null ? JsonConvert.SerializeObject(CustomData) : null;
            set => CustomData = value != null ? JsonConvert.DeserializeObject(value) : null;
        }
        /// <summary>
        /// The client object associated with the user.
        /// </summary>
        public virtual ClientSecret ClientSecret { get; set; }
    }
}
