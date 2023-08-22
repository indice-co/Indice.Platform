using System.ComponentModel.DataAnnotations;

namespace Indice.Features.Identity.Server.Totp.Models;

/// <summary>Verification request object.</summary>
public class TotpVerificationRequest
{
    /// <summary>The TOTP code.</summary>
    [Required]
    public string Code { get; set; } = string.Empty;
    /// <summary>Optionally pass the reason used to generate the TOTP.</summary>
    public string? Purpose { get; set; }
    /// <summary>The user authentication method to be used.</summary>
    public string? AuthenticationMethod { get; set; }
}
