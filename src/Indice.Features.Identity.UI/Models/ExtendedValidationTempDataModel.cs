namespace Indice.Features.Identity.UI.Models;

/// <summary>Temp data model for passing around state between views via cookies in extended validation.</summary>
public class ExtendedValidationTempDataModel
{
    /// <summary>The current user id.</summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary>An alert to be shown.</summary>
    public AlertModel? Alert { get; set; }
    /// <summary>The form should be disabled.</summary>
    public bool DisableForm { get; set; }
    /// <summary>Where to go next.</summary>
    public string? NextStepUrl { get; set; }
}
