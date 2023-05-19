namespace Indice.Features.Identity.UI.Models;

/// <summary>Input model for setting a new password while logged in.</summary>
public class AddPasswordInputModel
{
    /// <summary>The new password</summary>
    public string? NewPassword { get; set; }
    /// <summary>The new password confirmed</summary>
    public string? ConfirmPassword { get; set; }
}
