using Indice.Features.Identity.Core.Models;

namespace Indice.Features.Identity.UI.Models;

/// <summary>Input model for MFA on-boarding page.</summary>
public class MfaOnboardingInputModel
{
    /// <summary>The return URL.</summary>
    public string? ReturnUrl { get; set; }
    /// <summary>The selected authentication method.</summary>
    public AuthenticationMethodType SelectedAuthenticationMethod { get; set; }
}
