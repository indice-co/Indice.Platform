namespace Indice.Features.Identity.UI.Models;

/// <summary>Request input model for the edit profile details page.</summary>
public class ProfileDetailsInputModel
{
    /// <summary>The first name of the user.</summary>
    public string? FirstName { get; set; }
    /// <summary>The last name of the user.</summary>
    public string? LastName { get; set; }
    /// <summary>The Tax Identification Number (TIN) of the user.</summary>
    public string? Tin { get; set; }
    /// <summary>The birth date of the user.</summary>
    public DateTime? BirthDate { get; set; }
    /// <summary>The time zone information of the user.</summary>
    public string? ZoneInfo { get; set; }
    /// <summary>Indicates whether the user has given consent for commercial communication.</summary>
    public bool ConsentCommercial { get; set; }
    /// <summary>The values of the additional claims that are marked as user editable.</summary>
    public List<ProfileClaimInputModel> Claims { get; set; } = [];
}

/// <summary>The value of a user editable claim, posted from the edit profile details page.</summary>
public class ProfileClaimInputModel
{
    /// <summary>The claim type.</summary>
    public string? Type { get; set; }
    /// <summary>The claim value.</summary>
    public string? Value { get; set; }
}
