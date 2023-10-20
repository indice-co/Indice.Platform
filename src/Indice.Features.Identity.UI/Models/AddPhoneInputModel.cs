namespace Indice.Features.Identity.UI.Models;

/// <summary>The input model that backs the add phone page.</summary>
public class AddPhoneInputModel
{
    /// <summary>The phone number.</summary>
    public string? PhoneNumber { get; set; }
    /// <summary>The return URL.</summary>
    public string? ReturnUrl { get; set; }
    /// <summary>The calling code.</summary>
    public string? CallingCode { get; set; }
}
