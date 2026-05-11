namespace Indice.Features.Identity.UI.Models;

/// <summary>View model for the MFA onboarding recovery codes page.</summary>
public class RecoveryCodesViewModel
{
    /// <summary>The one-time recovery codes generated after enabling the authenticator app.</summary>
    public string[] RecoveryCodes { get; set; } = Array.Empty<string>();
    /// <summary>The email address of the user the codes belong to. Used for the downloaded file header.</summary>
    public string? UserEmail { get; set; }
    /// <summary>The return URL to continue to once the user acknowledges the codes.</summary>
    public string? ReturnUrl { get; set; }
}
