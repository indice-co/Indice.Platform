namespace Indice.AspNetCore.Identity.Models;

/// <summary>View model for setting a new password while logged in.</summary>
public class AddPasswordModel
{
    /// <summary>The new password</summary>
    public string NewPassword { get; set; }
    /// <summary>The new password confirmed</summary>
    public string ConfirmPassword { get; set; }
}
