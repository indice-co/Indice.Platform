namespace Indice.Features.Identity.UI.Models;
/// <summary>Request input model for the Adding an external provider link to the profile page.</summary>
public class LoginLinkInputModel
{
    /// <summary>External Provider name. Same as scheme name</summary>
    public string? LoginProvider { get; set; }
    /// <summary>External Provider key. The key with which it is linked to the user.</summary>
    public string? ProviderKey { get; set; }
}
