namespace Indice.Configuration
{
    /// <summary>Constant values for TOTP services.</summary>
    public static class TotpConstants
    {
        /// <summary>Token generation purpose.</summary>
        public static class TokenGenerationPurpose
        {
            /// <summary>Strong Customer Authentication.</summary>
            public const string StrongCustomerAuthentication = "Strong Customer Authentication";
            /// <summary>Two Factor Authentication.</summary>
            public const string TwoFactorAuthentication = "Two Factor Authentication";
            /// <summary>Session OTP.</summary>
            public const string SessionOtp = "Session OTP";
        }

        /// <summary>Grant type.</summary>
        public static class GrantType
        {
            /// <summary>TOTP custom grant type.</summary>

            public const string Totp = "totp";
        }
    }
}
