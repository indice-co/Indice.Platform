namespace Indice.Features.Identity.UI.Models;

/// <summary></summary>
public class EnableMfaSmsInputModel
{
    /// <summary></summary>
    public string? PhoneCallingCode { get; set; }
    /// <summary></summary>
    public string? PhoneNumber { get; set; }
    /// <summary></summary>
    public string? ReturnUrl { get; set; }
}
