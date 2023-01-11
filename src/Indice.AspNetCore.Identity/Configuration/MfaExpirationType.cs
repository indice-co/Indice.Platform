using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity.Configuration
{
    /// <summary>Type of expiration for <see cref="IdentityConstants.TwoFactorRememberMeScheme"/> cookie.</summary>
    public enum MfaExpirationType
    {
        /// <summary>Absolute expiration</summary>
        Absolute,
        /// <summary>Sliding expiration</summary>
        Sliding
    }
}
