using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Indice.Features.Identity.UI.Models;

/// <summary>Manage profile page view model.</summary>
public class ProfileViewModel : ProfileInputModel
{
    /// <summary>Indicates whether there is a pending email change. That requires verification</summary>
    public bool EmailChangePending { get; set; }
    /// <summary>List of associated external providers and their user login information.</summary>
    public IList<UserLoginInfo> CurrentLogins { get; set; } = [];
    /// <summary>List of available authentication providers. Non associated</summary>
    public IList<AuthenticationScheme> OtherLogins { get; set; } = [];
    /// <summary>List of available language preferences.</summary>
    public IList<CultureInfo> SupportedCultures { get; set; } = [];
    /// <summary>Indicates whether the user can remove external authentication providers.</summary>
    public bool CanRemoveProvider { get; set; }
    /// <summary>Indicates whether the user can edit their Tax Identification Number. This changes via the admin ui by marking a the well known claim type <c>tin</c> as UserEditable <c>false</c> </summary>
    public bool DisableEditTin { get; set; }
    /// <summary>Indicates whether to show concatenated Given name and last name as Fullname with disabled edit. This changes via the admin ui by marking both well known claim type <c>given_name</c> and <c>family_name</c>  as UserEditable <c>false</c> </summary>
    public bool DisableEditNameSurname { get; set; }
    /// <summary>Indicates whether the user has enabled Developer TOTP.</summary>
    public bool HasDeveloperTotp { get; set; }
}
