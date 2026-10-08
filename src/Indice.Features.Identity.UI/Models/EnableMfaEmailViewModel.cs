namespace Indice.Features.Identity.UI.Models;

/// <summary>
/// Represents the view model for enabling multi-factor authentication (MFA) via email, including information about
/// email confirmation status and workflow navigation.
/// </summary>
public class EnableMfaEmailViewModel : EnableMfaEmailInputModel
{
    /// <summary>Gets or sets a value indicating whether the user's email address has been confirmed.</summary>
    public bool EmailConfirmed { get; set; }
    /// <summary>Gets a value indicating whether the email input field should be disabled.</summary>
    public bool DisableEmailInput => EmailConfirmed;
    /// <summary>Gets or sets the URL of the next step in the workflow or process.</summary>
    public string? NextStepUrl { get; set; }
}
