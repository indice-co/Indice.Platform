using Indice.AspNetCore.Identity.Models;

namespace Indice.AspNetCore.Identity.Api.Models
{
    /// <summary>Identity Server UI configuration for the specified client.</summary>
    public class ClientThemeConfigResponse
    {
        /// <summary>JSON schema describing the properties to configure for the UI.</summary>
        public dynamic Schema { get; set; }
        /// <summary>Identity Server UI configuration for the specified client.</summary>
        public DefaultClientThemeConfig Data { get; set; }
    }
}
