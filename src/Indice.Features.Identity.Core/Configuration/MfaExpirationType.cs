using Microsoft.AspNetCore.Identity;

namespace Indice.Features.Identity.Core.Configuration;

/// <summary>Type of expiration for <see cref="IdentityConstants.TwoFactorRememberMeScheme"/> cookie.</summary>
public enum MfaExpirationType
{
    /// <summary>Absolute expiration</summary>
    Absolute,
    /// <summary>Sliding expiration</summary>
    Sliding
}
