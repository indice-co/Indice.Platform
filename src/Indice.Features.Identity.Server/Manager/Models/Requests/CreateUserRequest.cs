using System.ComponentModel.DataAnnotations;
using Indice.Features.Identity.Core.Data.Models;

namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Models a new user that will be created on the server.</summary>
public class CreateUserRequest
{
    /// <summary>The first name of the user.</summary>
    public string? FirstName { get; set; }
    /// <summary>The last name of the user.</summary>
    public string? LastName { get; set; }
    /// <summary>The username used to login.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Field 'UserName' is required.")]
    public string UserName { get; set; } = string.Empty;
    /// <summary>The email of the user.</summary>
    public string? Email { get; set; }
    /// <summary>Gets or sets a flag indicating if a user has confirmed their email address.</summary>
    public bool? EmailConfirmed { get; set; }
    /// <summary>The initial password of the user.</summary>
    public string? Password { get; set; }
    /// <summary>User's phone number.</summary>
    public string? PhoneNumber { get; set; }
    /// <summary>Gets or sets a flag indicating if a user has confirmed their telephone address.</summary>
    public bool? PhoneNumberConfirmed { get; set; }
    /// <summary>Represents the password expiration policy the value is measured in days.</summary>
    public PasswordExpirationPolicy? PasswordExpirationPolicy { get; set; }
    /// <summary>Forces the user to change his password after created by the system admin.</summary>
    public bool? ChangePasswordAfterFirstSignIn { get; set; }
    /// <summary>Bypasses all password validation rules.</summary>
    public bool? BypassPasswordValidation { get; set; }
    /// <summary>Gets or sets a flag indicating if two factor authentication is enabled for this user.</summary>
    public bool? TwoFactorEnabled { get; set; }
    /// <summary>Dynamic claims that have been marked as required.</summary>
    public List<BasicClaimInfo> Claims { get; set; } = new List<BasicClaimInfo>();
    /// <summary>The names of the roles that the user will be added to.</summary>
    public List<string>? Roles { get; set; } = new List<string>();
}
