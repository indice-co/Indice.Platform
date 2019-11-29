using Newtonsoft.Json;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// Contains various server settings for identity.
    /// </summary>
    public class SystemSettings
    {
        /// <summary>
        /// Primary key for setting.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Specifies options for password requirements.
        /// </summary>
        public dynamic Options { get; set; }
        /// <summary>
        /// Specifies options for password requirements in JSON format.
        /// </summary>
        public string OptionsJson {
            get => Options != null ? JsonConvert.SerializeObject(Options) : null;
            private set => Options = value != null ? JsonConvert.DeserializeObject(value) : null;
        }
    }
}
