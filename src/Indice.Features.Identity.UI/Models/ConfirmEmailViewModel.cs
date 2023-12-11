namespace Indice.Features.Identity.UI.Models;

/// <summary>Confir Email Page View Model</summary>
public class ConfirmEmailViewModel
{
    /// <summary>The email address to be verified</summary>
    public string? Email { get; set; }
    /// <summary>True when the email is already verified</summary>
    public bool AlreadyVerified { get; set; } = false;
    /// <summary>True when the email was just verified</summary>
    public bool Verified { get; set; } = false;
    /// <summary>True when token is invalid or expired</summary>
    public bool InvalidOrExpiredToken { get; set; } = false;
    /// <summary>The return url if any to automatically redirect</summary>
    public string? ReturnUrl { get; set; }
    /// <summary>True when the return url is not null and valid</summary>
    public bool HasReturnUrl => !string.IsNullOrEmpty(ReturnUrl);
}
