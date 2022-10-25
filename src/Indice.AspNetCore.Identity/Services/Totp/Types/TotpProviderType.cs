namespace Indice.AspNetCore.Identity
{
    /// <summary>Supported TOTP providers used for MFA.</summary>
    public enum TotpProviderType
    {
        /// <summary>Phone.</summary>
        Phone = 1,
        /// <summary>E-token.</summary>
        EToken = 3,
        /// <summary>Standard OTP.</summary>
        StandardOtp = 4
    }
}
