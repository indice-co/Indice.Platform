namespace Indice.Features.Identity.UI.Models;

/// <summary>The input model that backs the verify phone page.</summary>
public class VerifyEmailInputModel
{
    /// <summary>The verification code.</summary>
    public string? Code { get; set; }
    /// <summary>The email address.</summary>
    public string? Email { get; set; }
    /// <summary>The return URL.</summary>
    public string? ReturnUrl { get; set; }
    /// <summary> The flag for OTP resend process.</summary>
    public bool OtpResend { get; set; }
}
