using System;
using System.Collections.Generic;
using IdentityModel;
using Indice.AspNetCore.Identity.Data.Models;

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
        public const string Code = "code";
        public const string CodeChallenge = "code_challenge";
        public const string CodeChallengeMethod = "code_challenge_method";
        public const string CodeSignature = "code_signature";
        public const string CodeVerifier = "code_verifier";
        public const string DeviceId = "device_id";
        public const string DeviceName = "device_name";
        public const string DevicePlatform = "device_platform";
        public const string Mode = "mode";
        public const string OtpCode = "otp";
        public const string Pin = "pin";
        public const string PublicKey = "public_key";
    }
}
