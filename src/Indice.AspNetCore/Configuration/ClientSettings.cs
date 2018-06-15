namespace Indice.Configuration
{
    /// <summary>
    /// Settings used for Authentication of a client app.
    /// </summary>
    public class ClientSettings
    {
        /// <summary>
        /// The name is used to mark the section found inside a configuration file.
        /// </summary>
        public static readonly string Name = "Client";

        /// <summary>
        /// The client id
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// The client password
        /// </summary>
        public string ClientSecret { get; set; }
    }
}
