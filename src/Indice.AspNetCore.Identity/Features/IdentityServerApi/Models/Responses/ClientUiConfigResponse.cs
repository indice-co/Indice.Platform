namespace Indice.AspNetCore.Identity.Api.Models
{
    /// <summary>Identity Server UI configuration for the specified client.</summary>
    public class ClientUiConfigResponse
    {
        /// <summary>JSON schema describing the properties to configure for the UI.</summary>
        public dynamic Schema { get; set; }
        /// <summary>Identity Server UI configuration for the specified client.</summary>
        public ClientUiConfig Data { get; set; }
    }

    /// <summary>Identity Server UI configuration for the specified client.</summary>
    public class ClientUiConfig
    {
        /// <summary>The URL of the background image.</summary>
        public string BackgroundImage { get; set; }
        /// <summary>The background color.</summary>
        public string AccentColor { get; set; }
        /// <summary>A primary color.</summary>
        public string PrimaryColor { get; set; }
        /// <summary>A secondary color.</summary>
        public string SecondaryColor { get; set; }
    }
}
