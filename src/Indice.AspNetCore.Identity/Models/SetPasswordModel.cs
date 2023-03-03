namespace Indice.AspNetCore.Identity.Models;

/// <summary>View model for setting a new passord while logged in (Change password flow from the profile page)</summary>
public class SetPasswordModel
{
    /// <summary>The new password</summary>
    public string NewPassword { get; set; }

    /// <summary>The new password confirmed</summary>
    public string ConfirmPassword { get; set; }
}
