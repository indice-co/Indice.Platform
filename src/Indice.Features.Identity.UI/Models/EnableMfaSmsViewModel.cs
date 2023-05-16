namespace Indice.Features.Identity.UI.Models;

/// <summary></summary>
public class EnableMfaSmsViewModel : EnableMfaSmsInputModel
{
    /// <summary></summary>
    public bool PhoneNumberConfirmed { get; set; }
    /// <summary></summary>
    public bool DisablePhoneNumberInput => PhoneNumberConfirmed;
    /// <summary></summary>
    public string? NextStepUrl { get; set; }
}
