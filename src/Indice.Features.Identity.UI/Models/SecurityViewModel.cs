namespace Indice.Features.Identity.UI.Models;

/// <summary>View model for the security page.</summary>
public class SecurityViewModel
{
    /// <summary>The name displayed next to the profile picture (given and family name, or the username).</summary>
    public string? DisplayName { get; set; }
    /// <summary>The date the user account was created.</summary>
    public DateTimeOffset? CreateDate { get; set; }
    /// <summary>The date the user last signed in.</summary>
    public DateTimeOffset? LastSignInDate { get; set; }
    /// <summary>Indicates whether the user has a local password. When false, the password row links to <c>/AddPassword</c> instead of <c>/ChangePassword</c>.</summary>
    public bool HasPassword { get; set; }
    /// <summary>The date the user last changed their password.</summary>
    public DateTimeOffset? LastPasswordChangeDate { get; set; }
    /// <summary>The email address of the user.</summary>
    public string? Email { get; set; }
    /// <summary>Indicates whether the email address is verified.</summary>
    public bool EmailConfirmed { get; set; }
    /// <summary>Indicates whether the user can change their email address on <c>/ChangeEmail</c>.</summary>
    public bool CanEditEmail { get; set; }
    /// <summary>The phone number of the user.</summary>
    public string? PhoneNumber { get; set; }
    /// <summary>Indicates whether the phone number is verified.</summary>
    public bool PhoneNumberConfirmed { get; set; }
    /// <summary>Indicates whether the user can change their phone number on <c>/ChangePhone</c>.</summary>
    public bool CanEditPhone { get; set; }
    /// <summary>Indicates whether two-factor authentication is enabled for the user.</summary>
    public bool TwoFactorEnabled { get; set; }
    /// <summary>The display name (not localized) of the two-factor authentication method the user signs in with, when two-factor authentication is enabled.</summary>
    public string? TwoFactorMethodName { get; set; }
    /// <summary>Indicates whether the user can manage two-factor authentication on <c>/ManageMfa</c>.</summary>
    public bool CanManageMfa { get; set; }
}
