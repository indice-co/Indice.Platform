using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Indice.Features.Identity.UI.Models;

/// <summary>Manage profile page view model.</summary>
public class ProfileViewModel : ProfileInputModel
{
    /// <summary>Indicates whether there is a pending email change. That requires verification</summary>
    public bool EmailChangePending { get; set; }
    /// <summary>The date the user account was created.</summary>
    public DateTimeOffset? CreateDate { get; set; }
    /// <summary>The date the user last signed in.</summary>
    public DateTimeOffset? LastSignInDate { get; set; }
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
    ///<summary>Indicates whether the user can edit their family name. This changes via the admin ui by marking the well known claim type <c>family_name</c> as UserEditable <c>false</c> </summary>
    public bool DisableEditFamilyName { get; set; }
    ///<summary>Indicates whether the user can edit their given name. This changes via the admin ui by marking the well known claim type <c>given_name</c> as UserEditable <c>false</c> </summary>
    public bool DisableEditGivenName { get; set; }
    /// <summary>Indicates whether to show concatenated Given name and last name as Fullname with disabled edit. This changes via the admin ui by marking both well known claim type <c>given_name</c> and <c>family_name</c>  as UserEditable <c>false</c> </summary>
    public bool ShowFullName { get { return DisableEditFamilyName && DisableEditGivenName; } }
    /// <summary>Indicates whether the user has enabled Developer TOTP.</summary>
    public bool HasDeveloperTotp { get; set; }
    /// <summary>Indicates whether two-factor authentication is enabled for the user.</summary>
    public bool TwoFactorEnabled { get; set; }
    /// <summary>The display name (not localized) of the two-factor authentication method the user signs in with, when two-factor authentication is enabled.</summary>
    public string? TwoFactorMethodName { get; set; }
    /// <summary>Indicates whether the user can manage two-factor authentication in the profile pages.</summary>
    public bool CanManageMfa { get; set; }
    /// <summary>The additional claims, marked as user editable, that have no dedicated field on the profile page, with the user's current values.</summary>
    public IList<ProfileClaimViewModel> Claims { get; set; } = [];
}
