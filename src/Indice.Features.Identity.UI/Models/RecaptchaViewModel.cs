namespace Indice.Features.Identity.UI.Models;

/// <summary>View model for reCAPTCHA integration.</summary>
public class RecaptchaViewModel
{
    /// <summary>Gets or sets the form ID.</summary>
    public required string FormId { get; set; }
    /// <summary>Gets or sets the button ID.</summary>
    public required string ButtonId { get; set; }
    /// <summary>Gets or sets the reCAPTCHA action name.</summary>
    public required string Action { get; set; }
    /// <summary>Gets or sets a value indicating whether the form is a login form.</summary>
    public bool IsLoginForm { get; set; }
}