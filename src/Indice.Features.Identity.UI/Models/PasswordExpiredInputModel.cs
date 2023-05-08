namespace Indice.Features.Identity.UI.Models;

/// <summary>The input model that backs the password expired page.</summary>
public class PasswordExpiredInputModel
{
    /// <summary>The new password.</summary>
    public string? NewPassword { get; set; }
    /// <summary>The new password confirmed.</summary>
    public string? NewPasswordConfirmation { get; set; }
    /// <summary>The return URL.</summary>
    public string? ReturnUrl { get; set; }
}
