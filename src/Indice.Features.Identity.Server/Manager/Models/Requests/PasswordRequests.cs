using System.ComponentModel.DataAnnotations;

namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Models a password change request by the user.</summary>
public class ChangePasswordRequest
{
    /// <summary>The current password of the user.</summary>
    [Required(ErrorMessage = "Field 'OldPassword' is required.")]
    public string OldPassword { get; set; }
    /// <summary>The new password of the user.</summary>
    [Required(ErrorMessage = "Field 'NewPassword' is required.")]
    public string NewPassword { get; set; }
    /// <summary>The new password confirmation.</summary>
    [Compare("NewPassword", ErrorMessage = "Password confirmation is not correct.")]
    public string NewPasswordConfirmation { get; set; }
}

/// <summary>Models a request to set a user's password.</summary>
public class SetPasswordRequest
{
    /// <summary>The password of the user.</summary>
    [Required(ErrorMessage = "Field 'Password' is required.")]
    public string Password { get; set; }
    /// <summary>Forces the user to change his password after changed by the system admin.</summary>
    public bool? ChangePasswordAfterFirstSignIn { get; set; }
    /// <summary>Bypasses all password validation rules.</summary>
    public bool? BypassPasswordValidation { get; set; }
}
