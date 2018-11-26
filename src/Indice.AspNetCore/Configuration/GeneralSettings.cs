namespace Indice.Configuration
{
    /// <summary>
    /// General settings for an ASP.NET Core application.
    /// </summary>
    public class GeneralSettings
    {
        /// <summary>
        /// The name is used to mark the section found inside a configuration file.
        /// </summary>
        public static readonly string Name = "General";

        /// <summary>
        /// Url that the app is Hosted under.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The url for the IdentityServer.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// The name of the app. Usually used for the Layout page Title inside an Html header. 
        /// </summary>
        public string ApplicationName { get; set; } = "My App name";

        /// <summary>
        /// The name of the organization or brand.
        /// </summary>
        public string Organization { get; set; } = "Indice OE";
        
        /// <summary>
        /// The address of the organization or office location. Used in emails and website footers.
        /// </summary>
        public string OrganizationAddress { get; set; }

        /// <summary>
        /// A descritpion for the app.
        /// </summary>
        public string ApplicationDescription { get; set; } = "My App description.";

        /// <summary>
        /// API settings if API is present.
        /// </summary>
        public ApiSettings Api { get; set; }

        /// <summary>
        /// Swagger endpoint toggle.
        /// </summary>
        public bool EnableSwagger { get; set; }
    }
}
