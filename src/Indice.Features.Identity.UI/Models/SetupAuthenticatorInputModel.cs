namespace Indice.Features.Identity.UI.Models;

/// <summary>Input model for the MFA onboarding authenticator app setup page.</summary>
public class SetupAuthenticatorInputModel
{
    /// <summary>The verification code produced by the authenticator app.</summary>
    public string? Code { get; set; }
    /// <summary>The return URL.</summary>
    public string? ReturnUrl { get; set; }
}
