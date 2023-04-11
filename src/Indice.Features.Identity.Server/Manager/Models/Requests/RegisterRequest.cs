using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Models a new user that is registering on the system.</summary>
public class RegisterRequest
{
    /// <summary>The first name of the user.</summary>
    public string? FirstName { get; set; }
    /// <summary>The last name of the user.</summary>
    public string? LastName { get; set; }
    /// <summary>The username used to login.</summary>
    [Required]
    public string UserName { get; set; } = string.Empty;
    /// <summary>User password.</summary>
    [Required(AllowEmptyStrings = false)]
    public string? Password { get; set; }
    /// <summary>User password confirmation.</summary>
    [Compare(nameof(Password))]
    public string? PasswordConfirmation { get; set; }
    /// <summary>Email.</summary>
    [Required]
    public string? Email { get; set; }
    /// <summary>Phone number.</summary>
    public string? PhoneNumber { get; set; }
    /// <summary>Privacy policy read.</summary>
    public bool HasReadPrivacyPolicy { get; set; }
    /// <summary>Terms read.</summary>
    public bool HasAcceptedTerms { get; set; }
    /// <summary>User claims.</summary>
    public List<BasicClaimInfo> Claims { get; set; } = new List<BasicClaimInfo>();
}
