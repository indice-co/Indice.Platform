namespace Indice.Features.Identity.UI.Models;

public class ExtendedValidationTempDataModel
{
    public string UserId { get; set; }
    public AlertModel Alert { get; set; }
    public bool DisableForm { get; set; }
    public string? NextStepUrl { get; set; }
}
