namespace Indice.Features.Identity.UI.Models;

/// <summary>
/// Represents the view model for enabling multi-factor authentication (MFA) via SMS, including information about phone
/// number confirmation and workflow navigation.
/// </summary>
public class EnableMfaSmsViewModel : EnableMfaSmsInputModel
{
    /// <summary>Gets or sets a value indicating whether the user's PhoneNumber has been confirmed.</summary>
    public bool PhoneNumberConfirmed { get; set; }
    /// <summary>Gets a value indicating whether the PhoneNumber field should be disabled.</summary>
    public bool DisablePhoneNumberInput => PhoneNumberConfirmed;
    /// <summary>Gets or sets the URL of the next step in the workflow or process.</summary>
    public string? NextStepUrl { get; set; }
}
