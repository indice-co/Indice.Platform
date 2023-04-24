namespace Indice.Features.Identity.UI.Models;

/// <summary></summary>
public class ExtendedValidationTempDataModel
{
    /// <summary></summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary></summary>
    public AlertModel? Alert { get; set; }
    /// <summary></summary>
    public bool DisableForm { get; set; }
    /// <summary></summary>
    public string? NextStepUrl { get; set; }
}
