namespace Indice.Features.Identity.UI.Models;

/// <summary>Change password model.</summary>
public class ChangePasswordInputModel
{
    /// <summary>The original password.</summary>
    public string? OldPassword { get; set; }
    /// <summary>The new password.</summary>
    public string? NewPassword { get; set; }
}
