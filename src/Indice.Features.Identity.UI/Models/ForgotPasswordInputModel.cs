namespace Indice.Features.Identity.UI.Models;

/// <summary>Contains data required for forgot password process.</summary>
public class ForgotPasswordInputModel
{
    /// <summary>The user's email.</summary>
    public string? Email { get; set; }
    /// <summary>The URL to return to.</summary>
    public string? ReturnUrl { get; set; }
    /// <summary>reCAPTCHA token.</summary>
    public string? RecaptchaToken { get; set; }
    /// <summary>reCAPTCHA version (v2 or v3).</summary>
    public string? RecaptchaVersion { get; set; }
}
