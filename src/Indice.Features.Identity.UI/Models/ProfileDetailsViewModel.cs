namespace Indice.Features.Identity.UI.Models;

/// <summary>Edit profile details page view model.</summary>
public class ProfileDetailsViewModel
{
    /// <summary>Indicates whether the user can edit their Tax Identification Number. This changes via the admin ui by marking the well known claim type <c>tin</c> as UserEditable <c>false</c>.</summary>
    public bool DisableEditTin { get; set; }
    /// <summary>Indicates whether the user can edit their family name. This changes via the admin ui by marking the well known claim type <c>family_name</c> as UserEditable <c>false</c>.</summary>
    public bool DisableEditFamilyName { get; set; }
    /// <summary>Indicates whether the user can edit their given name. This changes via the admin ui by marking the well known claim type <c>given_name</c> as UserEditable <c>false</c>.</summary>
    public bool DisableEditGivenName { get; set; }
    /// <summary>Indicates whether to show the concatenated given name and family name as a read-only full name.</summary>
    public bool ShowFullName => DisableEditFamilyName && DisableEditGivenName;
    /// <summary>The additional claims that are marked as user editable, with their current values.</summary>
    public IList<ProfileClaimViewModel> Claims { get; set; } = [];
}
