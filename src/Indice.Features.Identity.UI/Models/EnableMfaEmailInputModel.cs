namespace Indice.Features.Identity.UI.Models;

/// <summary>The input model for enabling Email MFA.</summary>
public class EnableMfaEmailInputModel
{
    /// <summary>Gets or sets the email associated with sms mfa.</summary>
    public string? Email { get; set; }
    /// <summary>Gets or sets the URL to which the user is redirected after completing the current operation.</summary>
    public string? ReturnUrl { get; set; }
}
