namespace Indice.Features.Identity.UI.Models;

/// <summary>Request input model for the manage profile page.</summary>
public class ProfileInputModel
{
    /// <summary>The first name of the user.</summary>
    public string? FirstName { get; set; }
    /// <summary>The last name of the user.</summary>
    public string? LastName { get; set; }
    /// <summary>The username of the user.</summary>
    public string? UserName { get; set; }
    /// <summary>The email address of the user.</summary>
    public string? Email { get; set; }
    /// <summary>The international calling code associated with the user's phone number.</summary>
    public string? CallingCode { get; set; }
    /// <summary>The phone number of the user.</summary>
    public string? PhoneNumber { get; set; }
    /// <summary>The Tax Identification Number (TIN) of the user.</summary>
    public string? Tin { get; set; }
    /// <summary>The birth date of the user.</summary>
    public DateTime? BirthDate { get; set; }
    /// <summary>Indicates whether the user has given consent for commercial communication.</summary>
    public bool ConsentCommercial { get; set; }
    /// <summary>The date when the user gave consent for commercial communication.</summary>
    public DateTime? ConsentCommercialDate { get; set; }
    /// <summary>The developer's Time-based One-Time Password (TOTP) for the user.</summary>
    /// <remarks>This is a claim that if available can be used insted of a crypto generated one that is send via a communication channel.</remarks>
    public string? DeveloperTotp { get; set; }
    /// <summary>The time zone information of the user.</summary>
    public string? ZoneInfo { get; set; }

    /// <summary>A calculated field that holds the <see cref="PhoneNumber"/> padded with its international <seealso cref="CallingCode"/>.</summary>
    public string? PhoneNumberWithCallingCode => string.IsNullOrWhiteSpace(CallingCode) ? PhoneNumber : $"{CallingCode} {PhoneNumber}";

    /// <summary>Calculates a display name for UI purposes.</summary>
    public string? DisplayName => string.IsNullOrWhiteSpace(FirstName) ? UserName : (FirstName + " " + LastName).Trim();
}

