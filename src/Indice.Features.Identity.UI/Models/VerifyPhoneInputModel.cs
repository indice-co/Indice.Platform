namespace Indice.Features.Identity.UI.Models;

/// <summary>The input model that backs the verify phone page.</summary>
public class VerifyPhoneInputModel
{
    /// <summary>The verification code.</summary>
    public string? Code { get; set; }
    /// <summary>The phone number.</summary>
    public string? PhoneNumber { get; set; }
    /// <summary>The return URL.</summary>
    public string? ReturnUrl { get; set; }
}
