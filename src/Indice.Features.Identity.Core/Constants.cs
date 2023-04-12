namespace Indice.Features.Identity.Core;

/// <summary>Basic set of authorization policy names.</summary>
public class BasicPolicyNames
{
    /// <summary>Only a user marked as administrator in the user store or with a role assignment of the name 'Administrator' is allowed.</summary>
    public const string BeAdmin = nameof(BeAdmin);
}

/// <summary>Represents extended options you can use to configure the cookies middleware used by the identity system.</summary>
public class ExtendedIdentityConstants
{
    private const string CookiePrefix = "Identity";
    /// <summary>The scheme used to identify MFA on-boarding authentication cookies for round tripping user identities.</summary>
    public const string MfaOnboardingScheme = CookiePrefix + ".MfaOnboarding";
    /// <summary>The scheme used to identify extended validation authentication cookies for round tripping user identities.</summary>
    public const string ExtendedValidationUserIdScheme = CookiePrefix + ".ExtendedValidationUserId";
    /// <summary>The scheme used to identify Two Factor authentication cookies for round tripping user identities.</summary>
    public const string TwoFactorUserIdScheme = CookiePrefix + ".TwoFactorUserId";
    /// <summary>A claim type used to store temp data regarding password expiration that will be used in a partial login to identify that a user is in need for an immediate password change.</summary>
    public const string PasswordExpiredClaimType = "PasswordExpired";
}

/// <summary>Constant values for custom grants.</summary>
public class CustomGrantTypes
{
    /// <summary>Delegation.</summary>
    public const string Delegation = "delegation";
    /// <summary>Trusted device authorization.</summary>
    public const string DeviceAuthentication = "device_authentication";
    /// <summary>OTP authenticate.</summary>
    public const string OtpAuthenticate = "otp_authenticate";
}

/// <summary>Contains constants for client property keys.</summary>
public static class ClientPropertyKeys
{
    /// <summary>Key used in properties table for keeping translations for an object.</summary>
    public const string Translation = "translations";
    /// <summary>Key used in properties table for keeping translations for an object.</summary>
    public const string ThemeConfig = "ui-config";
}

/// <summary>Extra query parameters to be used in the authorize request.</summary>
public static class ExtraQueryParamNames
{
    /// <summary>A direction to display a different screen when a client asks for the authorize endpoint.</summary>
    public const string Operation = "operation";
}

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
        /// <summary>Multi factor authentication.</summary>
        public const string MultiFactorAuthentication = "mfa";
    }

    /// <summary>Grant type.</summary>
    public static class GrantType
    {
        /// <summary>TOTP custom grant type.</summary>
        public const string Totp = "totp";
    }
}

/// <summary>Constant values for IdentityServer features.</summary>
public class IdentityServerFeatures 
{
    /// <summary></summary>
    public const string Section = "IdentityServer:Features";
    /// <summary>Dashboard metrics.</summary>
    public const string DashboardMetrics = nameof(DashboardMetrics);
    /// <summary>Public registration.</summary>
    public const string PublicRegistration = nameof(PublicRegistration);
    /// <summary>Sign in logs.</summary>
    public const string SignInLogs = nameof(SignInLogs);
}
