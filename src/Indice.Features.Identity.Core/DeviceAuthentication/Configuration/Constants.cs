using IdentityModel;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Configuration
{
    internal class Constants
    {
        public static string DeviceAuthenticationOtpPurpose(string userId, string deviceId) => $"device-registration:{userId}:{deviceId}";

        public static readonly string[] ProtocolClaimsFilter = {
            JwtClaimTypes.AccessTokenHash,
            JwtClaimTypes.Audience,
            JwtClaimTypes.AuthorizedParty,
            JwtClaimTypes.AuthorizationCodeHash,
            JwtClaimTypes.ClientId,
            JwtClaimTypes.Expiration,
            JwtClaimTypes.IssuedAt,
            JwtClaimTypes.Issuer,
            JwtClaimTypes.JwtId,
            JwtClaimTypes.Nonce,
            JwtClaimTypes.NotBefore,
            JwtClaimTypes.ReferenceTokenId,
            JwtClaimTypes.SessionId,
            JwtClaimTypes.Scope
        };
    }

    internal static class RegistrationRequestParameters
    {
        public const string ClientId = "client_id";
        public const string Code = "code";
        public const string CodeChallenge = "code_challenge";
        public const string CodeSignature = "code_signature";
        public const string CodeVerifier = "code_verifier";
        public const string DeviceId = "device_id";
        public const string DeviceName = "device_name";
        public const string DevicePlatform = "device_platform";
        public const string Mode = "mode";
        public const string OtpCode = "otp";
        public const string Pin = "pin";
        public const string PublicKey = "public_key";
        public const string Scope = "scope";
        public const string DeliveryChannel = "channel";
        public const string RegistrationId = "registration_id";
    }

    internal static class ExtraTokenRequestErrors
    {
        public const string RequiresPassword = "requires_password";
    }
}
