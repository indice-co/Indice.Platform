using Microsoft.AspNetCore.Identity;

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
    /// <summary>The cookie prefix for all identity server cookies.</summary>
    public const string CookiePrefix = "idsrv";
    /// <summary>The scheme used to identify extended validation authentication cookies.</summary>
    public const string ExtendedValidationScheme = "Identity.ExtendedValidation";
    /// <summary>The cookie name used to identify extended validation authentication cookies for round tripping user identities.</summary>
    public const string ExtendedValidationCookieName = CookiePrefix + ".ev";
    /// <summary>The cookie name used to identify external authentication cookies.</summary>
    public const string ExternalCookieName = CookiePrefix + ".external";
    /// <summary>The cookie name used to identify Two Factor authentication cookies for round tripping user identities.</summary>
    public const string TwoFactorCookieName = CookiePrefix + ".mfa";
    /// <summary>This is essentialy a copy of the static resorce <see cref="IdentityConstants.TwoFactorUserIdScheme"/>. This is needed for attribute decoration</summary>
    public const string TwoFactorUserIdScheme = "Identity.TwoFactorUserId";
    /// <summary>The cookie name used to identify identify Two Factor authentication cookies for saving the Remember Me state.</summary>
    public const string TwoFactorRememberMeCookieName = CookiePrefix + ".mfa.remember";
    /// <summary>The cookie name used to identify to identify application authentication cookies.</summary>
    public const string ApplicationCookieName = CookiePrefix;
    /// <summary>A claim type used to store temp data regarding password expiration that will be used in a partial login to identify that a user is in need for an immediate password change.</summary>
    public const string PasswordExpiredClaimType = "password_expired";
    /// <summary>Authentication scheme name used by IdentityServer local API.</summary>
    public const string ApiAuthenticationScheme = "IdentityServerApiAccessToken";
    /// <summary>The cookie name used to protect agains XSS attacks.</summary>
    public const string AntiforgeryCookieName = CookiePrefix + ".xss";
    /// <summary>The cookie name used to by the CookieTempDataProvider to store temp parameters.</summary>
    public const string TempDataCookieName = CookiePrefix + ".tmp";
}

/// <summary>Constant values for custom grants.</summary>
public class CustomGrantTypes
{
    /// <summary>Delegation.</summary>
    public const string Delegation = "delegation";
    /// <summary>Trusted device authorization.</summary>
    public const string DeviceAuthentication = "device_authentication";
    /// <summary>OTP authenticate.</summary>
    public const string Mfa = "mfa";
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
    /// <summary>Impossible travel.</summary>
    public const string ImpossibleTravel = nameof(ImpossibleTravel);
    /// <summary>Activity logs.</summary>
    public const string ActivityLogs = nameof(ActivityLogs);
}


/// <summary>Rate limiting config for Identity Server API.</summary>
public static partial class RateLimiterPolicies
{
    /// <summary>Rate limiting policy for forgot password endpoint.</summary>
    public static readonly string ForgotPassword = "account/forgot-password";
    /// <summary>Rate limiting policy for forgot password confirmation endpoint.</summary>
    public static readonly string ForgotPasswordConfirmation = "account/forgot-password/confirmation";
    /// <summary>Rate limiting policy for password options endpoint.</summary>
    public static readonly string PasswordOptions = "account/password-options";
    /// <summary>Rate limiting policy for username exists endpoint.</summary>
    public static readonly string UserNameExists = "account/username-exists";
    /// <summary>Rate limiting policy for validate password endpoint.</summary>
    public static readonly string ValidatePassword = "account/validate-password";
    /// <summary>Rate limiting policy for TOTP endpoint.</summary>
    public static readonly string Totp = "totp";
    /// <summary>Rate limiting policy for calling codes endpoint.</summary>
    public static readonly string CallingCodes = "account/calling-codes";
    /// <summary>Rate limiting policy for upload picture endpoint.</summary>
    public static readonly string UploadPicture = "my/account/picture";
    /// <summary>Rate limiting policy for update email endpoint.</summary>
    public static readonly string UpdateEmail = "my/account/email";
    /// <summary>Rate limiting policy for update phone number endpoint.</summary>
    public static readonly string UpdatePhoneNumber = "my/account/phone-number";
    /// <summary>Rate limiting policy for change email endpoint.</summary>
    public static readonly string ChangeEmail = "my/account/email/change";
    /// <summary>Rate limiting policy for change phone number endpoint.</summary>
    public static readonly string ChangePhoneNumber = "my/account/phone-number/change";
    /// <summary>Rate limiting policy for email confirmation endpoint.</summary>
    public static readonly string EmailConfirmation = "my/account/email/confirmation";
    /// <summary>Rate limiting policy for phone number confirmation endpoint.</summary>
    public static readonly string PhoneNumberConfirmation = "my/account/phone-number/confirmation";
    /// <summary>Rate limiting policy for email change confirmation endpoint.</summary>
    public static readonly string EmailChangeConfirmation = "my/account/email/change-confirmation";
    /// <summary>Rate limiting policy for change phone number confirmation endpoint.</summary>
    public static readonly string ChangePhoneNumberConfirmation = "my/account/phone-number/change-confirmation";
    /// <summary>Rate limiting policy for a public secure pages.</summary>
    public static readonly string PublicPage = "secure-page";
    /// <summary>Rate limiting policy for register page.</summary>
    public static readonly string RegisterPage = "register";
    /// <summary>Rate limiting policy for forgot password page.</summary>
    public static readonly string ForgotPasswordPage = "forgot-password";
    /// <summary>Rate limiting policy for login page.</summary>
    public static readonly string LoginPage = "login";
    /// <summary>Rate limiting policy for add email page.</summary>
    public static readonly string LoginAddEmailPage = "login/add-email";
    /// <summary>Rate limiting policy for MFA onboarding add email page.</summary>
    public static readonly string MfaAddEmailPage = "login/mfa/onboarding/add-email";
    /// <summary>Rate limiting policy for recaptcha endpoint.</summary>
    public static readonly string RecaptchaEndpoint = "recaptchavalidate";
    /// <summary>Rate limiting policy for add phone page.</summary>
    public static readonly string AddPhonePage = "login/add-phone";
    /// <summary>Rate limiting policy for MFA onboarding add phone page.</summary>
    public static readonly string MfaAddPhonePage = "login/mfa/onboarding/add-phone";
    /// <summary>Rate limiting policy for profile page.</summary>
    public static readonly string ProfilePage = "manage/profile";
    /// <summary>Rate limiting policy for verify phone page.</summary>
    public static readonly string VerifyPhonePage = "login/verify-phone";
    

    /// <summary>All rate limiting policies.</summary>
    public static IReadOnlyList<string> All { get; } = new List<string> {
            ForgotPassword,
            ForgotPasswordConfirmation,
            PasswordOptions,
            UserNameExists,
            ValidatePassword,
            Totp,
            CallingCodes,
            UploadPicture,
            UpdateEmail,
            UpdatePhoneNumber,
            ChangeEmail,
            ChangePhoneNumber,
            EmailConfirmation,
            PhoneNumberConfirmation,
            EmailChangeConfirmation,
            ChangePhoneNumberConfirmation,
            PublicPage,
            RegisterPage,
            ForgotPasswordPage,
            LoginPage,
            LoginAddEmailPage,
            MfaAddEmailPage,
            RecaptchaEndpoint,
            AddPhonePage,
            MfaAddPhonePage,
            ProfilePage,
            VerifyPhonePage
        };
}