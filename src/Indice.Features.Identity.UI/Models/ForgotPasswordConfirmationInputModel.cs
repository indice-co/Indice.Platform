namespace Indice.Features.Identity.UI.Models;

/// <summary>Contains data required for forgot password confirmation process.</summary>
public class ForgotPasswordConfirmationInputModel
{
    /// <summary>The user's email.</summary>
    public string? Email { get; set; }
    /// <summary>The new password.</summary>
    public string? NewPassword { get; set; }
    /// <summary>The URL to return to.</summary>
    public string? ReturnUrl { get; set; }
    /// <summary>The token.</summary>
    public string? Token { get; set; }
}
