namespace Indice.Features.Identity.UI.Models;

/// <summary>A view model for the available strong customer authentication methods.</summary>
public class ScaMethodViewModel
{
    /// <summary>The name used to register the SCA method in the token providers list.</summary>
    public string? Name { get; set; }
    /// <summary>The display name.</summary>
    public string? DisplayName { get; set; }
    /// <summary>A boolean indicating the SCA method can generate TOTP codes or only validate them.</summary>
    public bool CanGenerateTotp { get; set; }
}
