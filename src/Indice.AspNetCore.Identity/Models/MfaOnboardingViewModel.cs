using System.Collections.Generic;
using Indice.Features.Identity.Core.Models;

namespace Indice.AspNetCore.Identity.Models;

/// <summary>A view model for MFA on-boarding page.</summary>
public class MfaOnboardingViewModel : MfaOnboardingInputModel
{
    /// <summary>The list of supported authentication methods.</summary>
    public IEnumerable<AuthenticationMethod> AuthenticationMethods { get; set; } = new List<AuthenticationMethod>();
}
