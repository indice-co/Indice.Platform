namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Models an user login provider.</summary>
public class UserLoginProviderInfo
{
    /// <summary>Provider name.</summary>
    public string? Name { get; set; }
    /// <summary>Provider key.</summary>
    public string? Key { get; set; }
    /// <summary>Provider display name.</summary>
    public string? DisplayName { get; set; }
}
