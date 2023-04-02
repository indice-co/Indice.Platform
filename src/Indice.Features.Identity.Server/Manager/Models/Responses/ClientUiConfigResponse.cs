using Indice.Features.Identity.Core.Models;

namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Identity Server UI configuration for the specified client.</summary>
public class ClientThemeConfigResponse
{
    /// <summary>JSON schema describing the properties to configure for the UI.</summary>
    public dynamic Schema { get; set; }
    /// <summary>Identity Server UI configuration for the specified client.</summary>
    public DefaultClientThemeConfig Data { get; set; }
}
