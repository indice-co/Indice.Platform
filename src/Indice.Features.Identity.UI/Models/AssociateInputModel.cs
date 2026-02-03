namespace Indice.Features.Identity.UI.Models;

/// <summary>The input model that backs the external provider association page.</summary>
public class AssociateInputModel
{
    /// <summary>The username.</summary>
    public string? UserName { get; set; }
    /// <summary>The email.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>The first name.</summary>
    public string FirstName { get; set; } = string.Empty;
    /// <summary>The last name.</summary>
    public string LastName { get; set; } = string.Empty;
    /// <summary>Phone number. </summary>
    public string? PhoneNumber { get; set; }
    /// <summary>User Has accepted the terms and conditions.</summary>
    public bool HasAcceptedTerms { get; set; }
    /// <summary>User has accepted the privacy policy.</summary>
    public bool HasReadPrivacyPolicy { get; set; }
    /// <summary>The return URL.</summary>
    public string? ReturnUrl { get; set; }
    /// <summary>Indicates whether to show concatenated Given name and last name as Fullname with disabled edit. This changes via the admin ui by marking both well known claim type <c>given_name</c> and <c>family_name</c>  as UserEditable <c>false</c> </summary>
    public bool DisableEditNameSurname { get; set; }
}
