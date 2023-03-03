namespace Indice.Features.Identity.UI.Areas.Identity.Models;

/// <summary>A model for the available Strong Customer Authentication methods.</summary>
public class ScaMethodModel
{
    /// <summary>The name used to register the SCA method in the token providers list.</summary>
    public string Name { get; set; }
    /// <summary>The display name.</summary>
    public string DisplayName { get; set; }
    /// <summary>A boolean indicating if the SCA method can generate TOTP codes or only validate them.</summary>
    public bool CanGenerateTotp { get; set; }
}
