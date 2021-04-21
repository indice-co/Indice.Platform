using System;
using System.Collections.Generic;
using IdentityModel;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Validation;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Configuration
{
    internal class Constants
    {
        public static readonly List<string> SupportedCodeChallengeMethods = new() {
            OidcConstants.CodeChallengeMethods.Sha256
        };

        public static string TrustedDeviceOtpPurpose(string userId, string deviceId) => $"trusted-device-registration:{userId}:{deviceId}";
    }

    internal static class RegistrationRequestParameters
    {
        public const string Mode = "mode";
        public const string DeviceId = "device_id";
        public const string CodeChallenge = "code_challenge";
        public const string CodeChallengeMethod = "code_challenge_method";
        public const string PublicKey = "public_key";
        public const string CodeVerifier = "code_verifier";
        public const string CodeSignature = "code_signature";
        public const string Code = "code";
        public const string OtpCode = "otp";

        public static InteractionMode? GetInteractionMode(string mode) {
            if (string.IsNullOrWhiteSpace(mode)) {
                return default;
            }
            if (mode.Equals("fingerprint", StringComparison.OrdinalIgnoreCase)) {
                return InteractionMode.Fingerprint;
            }
            if (mode.Equals("4pin", StringComparison.OrdinalIgnoreCase)) {
                return InteractionMode.FourPin;
            }
            return default;
        }
    }
}
