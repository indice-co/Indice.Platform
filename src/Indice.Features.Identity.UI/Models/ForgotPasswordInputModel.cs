namespace Indice.Features.Identity.UI.Models;

/// <summary>Contains data required for forgot password process.</summary>
public class ForgotPasswordInputModel
{
    /// <summary>The user's email.</summary>
    public string? Email { get; set; }
    /// <summary>The URL to return to.</summary>
    public string? ReturnUrl { get; set; }
}
