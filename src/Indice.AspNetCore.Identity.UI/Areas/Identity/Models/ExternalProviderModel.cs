namespace Indice.AspNetCore.Identity.UI.Areas.Identity.Models
{
    /// <summary>Class that models the properties of an external provider.</summary>
    public class ExternalProviderModel
    {
        /// <summary>The display name</summary>
        public string DisplayName { get; set; }
        /// <summary>The authentication scheme for the cookie</summary>
        public string AuthenticationScheme { get; set; }
    }
}
