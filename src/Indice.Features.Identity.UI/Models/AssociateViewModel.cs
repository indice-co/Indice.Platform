namespace Indice.Features.Identity.UI.Models;

/// <summary>The view model that backs the external provider association page.</summary>
public class AssociateViewModel : AssociateInputModel
{
    /// <summary>If the user is an existing one then this is the user id to associate him with. </summary>
    /// <remarks>It is better to trust this than the username especially in scenarios where <see cref="AssociateInputModel.UserName"/> is the same as the <see cref="AssociateInputModel.Email"/>.</remarks>
    public string UserId { get; set; } = string.Empty;
    /// <summary>The external id provider.</summary>
    public string Provider { get; set; } = string.Empty;
    /// <summary>Indicates whether the user can edit their family name. This changes via the admin ui by marking the well known claim type <c>family_name</c> as UserEditable <c>false</c> </summary>
    public bool DisableEditFamilyName { get; set; }
    /// <summary>Indicates whether the user can edit their given name. This changes via the admin ui by marking the well known claim type <c>given_name</c> as UserEditable <c>false</c> </summary>
    public bool DisableEditGivenName { get; set; }
    /// <summary>Indicates whether to show concatenated Given name and last name as Fullname with disabled edit. This changes via the admin ui by marking both well known claim type <c>given_name</c> and <c>family_name</c>  as UserEditable <c>false</c> </summary>
    public bool ShowFullName { get { return DisableEditFamilyName && DisableEditGivenName; } }
}
