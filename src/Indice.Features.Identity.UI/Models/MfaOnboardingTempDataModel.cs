using Indice.Features.Identity.Core.Models;

namespace Indice.Features.Identity.UI.Models;

/// <summary>Temp data model for passing around state between views via cookies in MFA onboarding.</summary>
public class MfaOnboardingTempDataModel
{
    /// <summary>The selected authentication method.</summary>
    public AuthenticationMethodType SelectedAuthenticationMethod { get; set; }
}
