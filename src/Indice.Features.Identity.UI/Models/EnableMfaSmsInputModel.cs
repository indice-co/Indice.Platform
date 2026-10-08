namespace Indice.Features.Identity.UI.Models;

/// <summary>The input model for enabling SMS MFA.</summary>
public class EnableMfaSmsInputModel
{
    /// <summary>Gets or sets the phone number associated with sms mfa.</summary>
    public string? PhoneNumber { get; set; }
    /// <summary>Gets or sets the URL to which the user is redirected after completing the current operation.</summary>
    public string? ReturnUrl { get; set; }
}
