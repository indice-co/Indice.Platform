using System.Security.Claims;
using System.Text;
using IdentityModel;
using Indice.Features.Identity.Core.Configuration;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.PasswordValidation;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using UAParser;

namespace Indice.Features.Identity.Core;

/// <summary>Provides the APIs for user sign in.</summary>
/// <typeparam name="TUser">The type encapsulating a user.</typeparam>
public class ExtendedSignInManager<TUser> : SignInManager<TUser> where TUser : User
{
    /// <summary>Default duration in days for two-factor remember.</summary>
    public const int DEFAULT_MFA_REMEMBER_DURATION_IN_DAYS = 90;
    private const string LOGIN_PROVIDER_KEY = "LoginProvider";
    private const string XSRF_KEY = "XsrfId";
    private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;
    private readonly IUserStore<TUser> _userStore;
    private readonly IMfaDeviceIdResolver _mfaDeviceIdResolver;

    /// <summary>Creates a new instance of <see cref="SignInManager{TUser}" /></summary>
    /// <param name="userManager">An instance of <see cref="UserManager{TUser}"/> used to retrieve users from and persist users.</param>
    /// <param name="contextAccessor">The accessor used to access the <see cref="HttpContext"/>.</param>
    /// <param name="claimsFactory">The factory to use to create claims principals for a user.</param>
    /// <param name="optionsAccessor">The accessor used to access the <see cref="IdentityOptions"/>.</param>
    /// <param name="logger">The logger used to log messages, warnings and errors.</param>
    /// <param name="schemes">The scheme provider that is used enumerate the authentication schemes.</param>
    /// <param name="confirmation">The <see cref="IUserConfirmation{TUser}"/> used check whether a user account is confirmed.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="authenticationSchemeProvider">Responsible for managing what authenticationSchemes are supported.</param>
    /// <param name="userStore">Provides an abstraction for a store which manages user accounts.</param>
    /// <param name="mfaDeviceIdResolver">An abstraction used to specify the way to resolve the device identifier used for MFA.</param>
    public ExtendedSignInManager(
        ExtendedUserManager<TUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<TUser> claimsFactory,
        IOptionsSnapshot<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<TUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<TUser> confirmation,
        IConfiguration configuration,
        IAuthenticationSchemeProvider authenticationSchemeProvider,
        IUserStore<TUser> userStore,
        IMfaDeviceIdResolver mfaDeviceIdResolver
    ) : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation) {
        _authenticationSchemeProvider = authenticationSchemeProvider ?? throw new ArgumentNullException(nameof(authenticationSchemeProvider));
        _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
        _mfaDeviceIdResolver = mfaDeviceIdResolver ?? throw new ArgumentNullException(nameof(mfaDeviceIdResolver));
        EnforceMfa = configuration.GetIdentityOption<bool?>($"{nameof(IdentityOptions.SignIn)}:Mfa", "Enforce") == true;
        RequirePostSignInConfirmedEmail = configuration.GetIdentityOption<bool?>(nameof(IdentityOptions.SignIn), nameof(RequirePostSignInConfirmedEmail)) == true;
        RequirePostSignInConfirmedPhoneNumber = configuration.GetIdentityOption<bool?>(nameof(IdentityOptions.SignIn), nameof(RequirePostSignInConfirmedPhoneNumber)) == true;
        ExpireBlacklistedPasswordsOnSignIn = configuration.GetIdentityOption<bool?>(nameof(IdentityOptions.SignIn), nameof(ExpireBlacklistedPasswordsOnSignIn)) == true;
        ExternalScheme = configuration.GetIdentityOption<string>(nameof(IdentityOptions.SignIn), nameof(ExternalScheme)) ?? IdentityConstants.ExternalScheme;
        PersistTrustedBrowsers = configuration.GetIdentityOption<bool?>($"{nameof(IdentityOptions.SignIn)}:Mfa", nameof(PersistTrustedBrowsers)) == true;
        MfaRememberDurationInDays = configuration.GetIdentityOption<int?>($"{nameof(IdentityOptions.SignIn)}:Mfa", "RememberDurationInDays") ?? DEFAULT_MFA_REMEMBER_DURATION_IN_DAYS;
        RememberTrustedBrowserAcrossSessions = configuration.GetIdentityOption<bool?>($"{nameof(IdentityOptions.SignIn)}:Mfa", nameof(RememberTrustedBrowserAcrossSessions)) == true;
        RememberExpirationType = configuration.GetIdentityOption<MfaExpirationType?>($"{nameof(IdentityOptions.SignIn)}:Mfa", nameof(RememberExpirationType)) ?? default;
    }

    private ExtendedUserManager<TUser> ExtendedUserManager => (ExtendedUserManager<TUser>)UserManager;
    /// <summary>Enables the feature post login email confirmation.</summary>
    public bool RequirePostSignInConfirmedEmail { get; }
    /// <summary>Enables the feature post login phone number confirmation.</summary>
    public bool RequirePostSignInConfirmedPhoneNumber { get; }
    /// <summary>If enabled then users with blacklisted passwords will be forced to change their password upon sign-in instead of waiting for the next time they need to change it.</summary>
    public bool ExpireBlacklistedPasswordsOnSignIn { get; }
    /// <summary>Enforces multi factor authentication for all users.</summary>
    public bool EnforceMfa { get; }
    /// <summary>The scheme used to identify external authentication cookies.</summary>
    public string ExternalScheme { get; }
    /// <summary>Decides whether a trusted browser should be stored in the <see cref="UserDevice"/> table.</summary>
    public bool PersistTrustedBrowsers { get; }
    /// <summary>Defines the number of days that the browser will remember the MFA action and will not require re-authentication.</summary>
    public int MfaRememberDurationInDays { get; }
    /// <summary>Defines whether to remember device even if a relevant cookie does not exist.</summary>
    public bool RememberTrustedBrowserAcrossSessions { get; }
    /// <summary>Type of expiration for <see cref="IdentityConstants.TwoFactorRememberMeScheme"/> cookie.</summary>
    public MfaExpirationType RememberExpirationType { get; }

    #region Method Overrides
    /// <inheritdoc/>
    public override async Task<ExternalLoginInfo> GetExternalLoginInfoAsync(string expectedXsrf = null) {
        var auth = await Context.AuthenticateAsync(ExternalScheme);
        var items = auth?.Properties?.Items;
        if (auth?.Principal == null || items == null || !items.ContainsKey(LOGIN_PROVIDER_KEY)) {
            return null;
        }
        if (expectedXsrf != null) {
            if (!items.ContainsKey(XSRF_KEY)) {
                return null;
            }
            var userId = items[XSRF_KEY];
            if (userId != expectedXsrf) {
                return null;
            }
        }
        var providerKey = auth.Principal.FindFirstValue(Options.ClaimsIdentity.UserIdClaimType);
        if (providerKey == null || items[LOGIN_PROVIDER_KEY] is not string provider) {
            return null;
        }
        var providerDisplayName = (await GetExternalAuthenticationSchemesAsync()).FirstOrDefault(p => p.Name == provider)?.DisplayName ?? provider;
        return new ExternalLoginInfo(auth.Principal, provider, providerKey, providerDisplayName) {
            AuthenticationTokens = auth.Properties.GetTokens()
        };
    }

    /// <inheritdoc/>
    public override async Task SignInAsync(TUser user, AuthenticationProperties authenticationProperties, string authenticationMethod = null) {
        await base.SignInAsync(user, authenticationProperties, authenticationMethod);
        if (user is User) {
            user.LastSignInDate = DateTimeOffset.UtcNow;
            await UserManager.UpdateAsync(user);
        }
    }

    /// <inheritdoc/>
    public override async Task<bool> CanSignInAsync(TUser user) {
        if (user is User && user.Blocked) {
            Logger.LogWarning(0, "User {userId} cannot sign in. User is blocked by the administrator.", await UserManager.GetUserIdAsync(user));
            return false;
        }
        return await base.CanSignInAsync(user);
    }

    /// <inheritdoc/>
    protected override async Task<SignInResult> SignInOrTwoFactorAsync(TUser user, bool isPersistent, string loginProvider = null, bool bypassTwoFactor = false) {
        var isEmailConfirmed = await UserManager.IsEmailConfirmedAsync(user);
        var isPhoneConfirmed = await UserManager.IsPhoneNumberConfirmedAsync(user);
        var userClaims = await UserManager.GetClaimsAsync(user);
        var firstName = userClaims.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value;
        var lastName = userClaims.FirstOrDefault(x => x.Type == JwtClaimTypes.FamilyName)?.Value;
        var isPasswordExpired = user.HasExpiredPassword();
        var doPartialSignIn = (!isEmailConfirmed && RequirePostSignInConfirmedEmail)
                           || (!isPhoneConfirmed && RequirePostSignInConfirmedPhoneNumber)
                           || isPasswordExpired;
        if (doPartialSignIn) {
            // Store the userId for use after two factor check.
            var userId = await UserManager.GetUserIdAsync(user);
            var returnUrl = Context.Request.Query["ReturnUrl"];
            await Context.SignInAsync(ExtendedIdentityConstants.ExtendedValidationUserIdScheme, StoreValidationInfo(userId, isEmailConfirmed, isPhoneConfirmed, isPasswordExpired, firstName, lastName), new AuthenticationProperties {
                RedirectUri = returnUrl,
                IsPersistent = isPersistent
            });
            var requiresEmailValidation = !isEmailConfirmed && RequirePostSignInConfirmedEmail;
            var requiresPhoneNumberValidation = !isPhoneConfirmed && RequirePostSignInConfirmedPhoneNumber;
            var requiresPasswordChange = isPasswordExpired;
            return new ExtendedSigninResult(requiresEmailValidation, requiresPhoneNumberValidation, requiresPasswordChange);
        }
        if (EnforceMfa && !bypassTwoFactor) {
            await Context.SignInAsync(IdentityConstants.TwoFactorUserIdScheme, StoreTwoFactorInfo(user.Id, loginProvider));
            return SignInResult.TwoFactorRequired;
        }
        var signInResult = await base.SignInOrTwoFactorAsync(user, isPersistent, loginProvider, bypassTwoFactor);
        if (signInResult.Succeeded && (user is User)) {
            user.LastSignInDate = DateTimeOffset.UtcNow;
            await UserManager.UpdateAsync(user);
            if (RememberExpirationType == MfaExpirationType.Sliding) {
                var authenticateResult = await Context.AuthenticateAsync(IdentityConstants.TwoFactorRememberMeScheme);
                if (authenticateResult.Succeeded && authenticateResult.Principal is not null) {
                    await RememberTwoFactorClientAsync(user);
                }
            }
        }
        return signInResult;
    }

    /// <inheritdoc/>
    public override async Task<SignInResult> TwoFactorSignInAsync(string provider, string code, bool isPersistent, bool rememberClient) {
        var twoFactorInfo = await RetrieveTwoFactorInfoAsync();
        if (twoFactorInfo == null || twoFactorInfo.UserId == null) {
            return SignInResult.Failed;
        }
        var user = await UserManager.FindByIdAsync(twoFactorInfo.UserId);
        if (user == null) {
            return SignInResult.Failed;
        }
        var error = await PreSignInCheck(user);
        if (error != null) {
            return error;
        }
        if (await UserManager.VerifyTwoFactorTokenAsync(user, provider, code)) {
            await DoTwoFactorSignInAsync(user, twoFactorInfo, isPersistent, rememberClient);
            return SignInResult.Success;
        }
        if (UserManager.SupportsUserLockout) {
            await UserManager.AccessFailedAsync(user);
        }
        return SignInResult.Failed;
    }

    /// <inheritdoc/>
    public override async Task<TUser> GetTwoFactorAuthenticationUserAsync() {
        var info = await RetrieveTwoFactorInfoAsync();
        if (string.IsNullOrWhiteSpace(info?.UserId)) {
            return default;
        }
        return await ExtendedUserManager.FindByIdAsync(info.UserId);
    }

    /// <inheritdoc/>
    public async override Task SignOutAsync() {
        var schemes = await _authenticationSchemeProvider.GetAllSchemesAsync();
        // Check if authentication scheme is registered before trying to sign out, to avoid errors.
        if (schemes.Any(x => x.Name == ExtendedIdentityConstants.ExtendedValidationUserIdScheme)) {
            await Context.SignOutAsync(ExtendedIdentityConstants.ExtendedValidationUserIdScheme);
        }
        if (schemes.Any(x => x.Name == ExternalScheme)) {
            await Context.SignOutAsync(ExternalScheme);
        }
        await base.SignOutAsync();
    }

    /// <inheritdoc/>
    public override async Task<SignInResult> CheckPasswordSignInAsync(TUser user, string password, bool lockoutOnFailure) {
        var attempt = await base.CheckPasswordSignInAsync(user, password, lockoutOnFailure);
        if (attempt.Succeeded && ExpireBlacklistedPasswordsOnSignIn) {
            var blacklistPasswordValidator = UserManager.PasswordValidators.OfType<NonCommonPasswordValidator<TUser>>().FirstOrDefault();
            if (blacklistPasswordValidator is not null && await blacklistPasswordValidator.IsBlacklistedAsync(password)) {
                // If blacklisted then expire users password before proceeding.
                await ExtendedUserManager.SetPasswordExpiredAsync(user, true);
            }
        }
        return attempt;
    }

    /// <inheritdoc/>
    public override AuthenticationProperties ConfigureExternalAuthenticationProperties(string provider, string redirectUrl, string userId = null) {
        var props = base.ConfigureExternalAuthenticationProperties(provider, redirectUrl, userId);
        var queryString = QueryHelpers.ParseNullableQuery(redirectUrl);
        // Make available the 'prompt' parameter to the downstream identity provider so that the client can have control over the re-authentication process.
        // This merely adds the item to the authentication properties.
        // The next thing to do is to configure the OpenIdConnect middleware to pass it on.
        if (queryString.ContainsKey("prompt")) {
            props.Items.Add("prompt", queryString["prompt"]);
        }
        return props;
    }

    /// <inheritdoc/>
    public override async Task RememberTwoFactorClientAsync(TUser user) {
        var principal = await StoreRememberClient(user);
        await Context.SignInAsync(IdentityConstants.TwoFactorRememberMeScheme, principal, new AuthenticationProperties { IsPersistent = true });
    }

    /// <inheritdoc/>
    public override async Task<bool> IsTwoFactorClientRememberedAsync(TUser user) {
        var userId = await UserManager.GetUserIdAsync(user);
        var result = await Context.AuthenticateAsync(IdentityConstants.TwoFactorRememberMeScheme);
        var isRemembered = result?.Principal is not null && result.Principal.FindFirstValue(JwtClaimTypes.Name) == userId;
        var deviceId = await _mfaDeviceIdResolver.Resolve();
        if (!string.IsNullOrWhiteSpace(deviceId) && (isRemembered || (!isRemembered && RememberTrustedBrowserAcrossSessions))) {
            var device = await ExtendedUserManager.GetDeviceByIdAsync(user, deviceId);
            isRemembered = device is not null && device.MfaSessionExpirationDate.HasValue && device.MfaSessionExpirationDate.Value > DateTimeOffset.UtcNow;
            return isRemembered;
        }
        return isRemembered;
    }
    #endregion

    #region Custom Methods
    /// <summary>Revokes all sessions for user browsers.</summary>
    /// <param name="user"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public Task<IdentityResult> RevokeMfaSessionsAsync(TUser user) {
        if (user is null) {
            throw new ArgumentNullException(nameof(user));
        }
        var deviceStore = GetDeviceStore();
        return deviceStore.SetBrowsersMfaSessionExpirationDate(user, null);
    }
    #endregion

    #region Helper Methods
    private static ClaimsPrincipal StoreValidationInfo(string userId, bool isEmailConfirmed, bool isPhoneConfirmed, bool isPasswordExpired, string firstName, string lastName) {
        var identity = new ClaimsIdentity(ExtendedIdentityConstants.ExtendedValidationUserIdScheme);
        identity.AddClaim(new Claim(JwtClaimTypes.Subject, userId));
        identity.AddClaim(new Claim(JwtClaimTypes.EmailVerified, isEmailConfirmed.ToString().ToLower()));
        identity.AddClaim(new Claim(JwtClaimTypes.PhoneNumberVerified, isPhoneConfirmed.ToString().ToLower()));
        identity.AddClaim(new Claim(ExtendedIdentityConstants.PasswordExpiredClaimType, isPasswordExpired.ToString().ToLower()));
        if (!string.IsNullOrWhiteSpace(firstName)) {
            identity.AddClaim(new Claim(JwtClaimTypes.GivenName, firstName));
        }
        if (!string.IsNullOrWhiteSpace(lastName)) {
            identity.AddClaim(new Claim(JwtClaimTypes.FamilyName, lastName));
        }
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal StoreTwoFactorInfo(string userId, string loginProvider) {
        var identity = new ClaimsIdentity(IdentityConstants.TwoFactorUserIdScheme);
        identity.AddClaim(new Claim(ClaimTypes.Name, userId));
        if (loginProvider != null) {
            identity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, loginProvider));
        }
        return new ClaimsPrincipal(identity);
    }

    private async Task<ClaimsPrincipal> StoreRememberClient(TUser user) {
        var deviceId = await _mfaDeviceIdResolver.Resolve();
        if (PersistTrustedBrowsers && !string.IsNullOrWhiteSpace(deviceId)) {
            var device = await ExtendedUserManager.GetDeviceByIdAsync(user, deviceId);
            if (device is not null) {
                device.IsTrusted = true;
                device.TrustActivationDate = device.TrustActivationDate ?? DateTimeOffset.UtcNow;
                device.MfaSessionExpirationDate = DateTimeOffset.UtcNow.AddDays(MfaRememberDurationInDays);
                await ExtendedUserManager.UpdateDeviceAsync(user, device);
            } else {
                var userAgent = Context.Request.Headers[HeaderNames.UserAgent];
                ClientInfo clientInfo = null;
                if (!string.IsNullOrWhiteSpace(userAgent)) {
                    var uaParser = Parser.GetDefault();
                    clientInfo = uaParser.Parse(userAgent);
                }
                var osInfo = FormatOsInfo(clientInfo?.OS);
                var name = $"{FormatUserAgentInfo(clientInfo?.UA)} on {osInfo}".Trim();
                device = new UserDevice {
                    DateCreated = DateTimeOffset.UtcNow,
                    DeviceId = deviceId,
                    IsTrusted = true,
                    MfaSessionExpirationDate = DateTimeOffset.UtcNow.AddDays(90),
                    Model = FormatDeviceInfo(clientInfo?.Device),
                    Name = name == string.Empty ? null : name,
                    OsVersion = osInfo,
                    Platform = DecideDevicePlatform(osInfo),
                    TrustActivationDate = DateTimeOffset.UtcNow,
                    ClientType = DeviceClientType.Browser,
                    User = user,
                    UserId = user.Id
                };
                await ExtendedUserManager.CreateDeviceAsync(user, device);
            }
        }
        var userId = await UserManager.GetUserIdAsync(user);
        var rememberBrowserIdentity = new ClaimsIdentity(IdentityConstants.TwoFactorRememberMeScheme);
        rememberBrowserIdentity.AddClaim(new Claim(JwtClaimTypes.Name, userId));
        if (UserManager.SupportsUserSecurityStamp) {
            var stamp = await UserManager.GetSecurityStampAsync(user);
            rememberBrowserIdentity.AddClaim(new Claim(Options.ClaimsIdentity.SecurityStampClaimType, stamp));
        }
        return new ClaimsPrincipal(rememberBrowserIdentity);
    }

    private async Task<TwoFactorAuthenticationInfo> RetrieveTwoFactorInfoAsync() {
        var result = await Context.AuthenticateAsync(ExtendedIdentityConstants.TwoFactorUserIdScheme);
        var claimsPrincipal = result?.Principal;
        if (claimsPrincipal is null) {
            return default;
        }
        var userId = claimsPrincipal.FindFirstValue(Options.ClaimsIdentity.UserNameClaimType) ??
                     claimsPrincipal.FindFirstValue(ClaimTypes.Name);
        var authenticationMethod = claimsPrincipal.FindFirstValue(JwtClaimTypes.AuthenticationMethod) ??
                                   claimsPrincipal.FindFirstValue(ClaimTypes.AuthenticationMethod);
        return new TwoFactorAuthenticationInfo {
            UserId = userId,
            LoginProvider = authenticationMethod
        };
    }

    private async Task DoTwoFactorSignInAsync(TUser user, TwoFactorAuthenticationInfo twoFactorInfo, bool isPersistent, bool rememberClient) {
        await ResetLockout(user);
        var claims = new List<Claim> {
            new Claim("amr", "mfa")
        };
        if (twoFactorInfo.LoginProvider is not null) {
            claims.Add(new Claim(ClaimTypes.AuthenticationMethod, twoFactorInfo.LoginProvider));
            await Context.SignOutAsync(IdentityConstants.ExternalScheme);
        }
        await Context.SignOutAsync(ExtendedIdentityConstants.TwoFactorUserIdScheme);
        if (rememberClient) {
            await RememberTwoFactorClientAsync(user);
        }
        await SignInWithClaimsAsync(user, isPersistent, claims);
    }

    private static string FormatUserAgentInfo(UserAgent userAgent) {
        if (userAgent is null) {
            return default;
        }
        var stringBuilder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(userAgent.Family)) {
            stringBuilder.Append(userAgent.Family);
        }
        if (!string.IsNullOrWhiteSpace(userAgent.Major)) {
            stringBuilder.Append($" {userAgent.Major}");
        }
        if (!string.IsNullOrWhiteSpace(userAgent.Minor)) {
            stringBuilder.Append($".{userAgent.Minor}");
        }
        if (!string.IsNullOrWhiteSpace(userAgent.Patch)) {
            stringBuilder.Append($".{userAgent.Patch}");
        }
        var userAgentInfo = stringBuilder.ToString().Trim();
        return userAgentInfo == string.Empty ? null : userAgentInfo;
    }

    private static string FormatOsInfo(OS os) {
        if (os is null) {
            return default;
        }
        var stringBuilder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(os.Family)) {
            stringBuilder.Append(os.Family);
        }
        if (!string.IsNullOrWhiteSpace(os.Major)) {
            stringBuilder.Append($" {os.Major}");
        }
        if (!string.IsNullOrWhiteSpace(os.Minor)) {
            stringBuilder.Append($".{os.Minor}");
        }
        if (!string.IsNullOrWhiteSpace(os.Patch)) {
            stringBuilder.Append($".{os.Patch}");
        }
        if (!string.IsNullOrWhiteSpace(os.PatchMinor)) {
            stringBuilder.Append($".{os.PatchMinor}");
        }
        var osInfo = stringBuilder.ToString().Trim();
        return osInfo == string.Empty ? null : osInfo;
    }

    private static string FormatDeviceInfo(Device device) {
        if (device is null) {
            return default;
        }
        var stringBuilder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(device.Family)) {
            stringBuilder.Append(device.Family);
        }
        if (!string.IsNullOrWhiteSpace(device.Brand)) {
            stringBuilder.Append($" {device.Brand}");
        }
        if (!string.IsNullOrWhiteSpace(device.Model)) {
            stringBuilder.Append($" {device.Model}");
        }
        var deviceInfo = stringBuilder.ToString().Trim();
        return deviceInfo == string.Empty ? null : deviceInfo;
    }

    private static DevicePlatform DecideDevicePlatform(string osInfo) {
        var devicePlatform = DevicePlatform.None;
        switch (osInfo) {
            case string x when x.Contains("iPhone", StringComparison.OrdinalIgnoreCase):
                devicePlatform = DevicePlatform.iOS;
                break;
            case string x when x.Contains("Android", StringComparison.OrdinalIgnoreCase):
                devicePlatform = DevicePlatform.Android;
                break;
            case string x when x.Contains("Windows", StringComparison.OrdinalIgnoreCase):
                devicePlatform = DevicePlatform.Windows;
                break;
            case string x when x.Contains("Linux", StringComparison.OrdinalIgnoreCase):
                devicePlatform = DevicePlatform.Linux;
                break;
            case string x when x.Contains("Mac", StringComparison.OrdinalIgnoreCase):
                devicePlatform = DevicePlatform.MacOS;
                break;
        }
        return devicePlatform;
    }

    private IUserDeviceStore<TUser> GetDeviceStore(bool throwOnFail = true) {
        var cast = _userStore as IUserDeviceStore<TUser>;
        if (throwOnFail && cast is null) {
            throw new NotSupportedException($"Store does not implement {nameof(IUserDeviceStore<TUser>)}.");
        }
        return cast;
    }
    #endregion
}

/// <summary>Extends the <see cref="SignInResult"/> type.</summary>
public class ExtendedSigninResult : SignInResult
{
    /// <summary>Construct an instance of <see cref="ExtendedSigninResult"/>.</summary>
    public ExtendedSigninResult(bool requiresEmailValidation, bool requiresPhoneNumberValidation, bool requiresPasswordChange) {
        RequiresEmailValidation = requiresEmailValidation;
        RequiresPhoneNumberValidation = requiresPhoneNumberValidation;
        RequiresPasswordChange = requiresPasswordChange;
    }

    /// <summary>Returns a flag indication whether the user attempting to sign-in requires phone number confirmation.</summary>
    /// <value>True if the user attempting to sign-in requires phone number confirmation, otherwise false.</value>
    public bool RequiresPhoneNumberValidation { get; }
    /// <summary>Returns a flag indication whether the user attempting to sign-in requires email confirmation.</summary>
    /// <value>True if the user attempting to sign-in requires email confirmation, otherwise false.</value>
    public bool RequiresEmailValidation { get; }
    /// <summary>Returns a flag indication whether the user attempting to sign-in requires an immediate password change due to expiration.</summary>
    /// <value>True if the user attempting to sign-in requires a password change, otherwise false.</value>
    public bool RequiresPasswordChange { get; }
}

/// <summary>Extensions on <see cref="SignInResult"/> type.</summary>
public static class ExtendedSignInManagerExtensions
{
    /// <summary>Returns a flag indication whether the user attempting to sign-in requires phone number confirmation.</summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool RequiresPhoneNumberConfirmation(this SignInResult result) => (result as ExtendedSigninResult)?.RequiresPhoneNumberValidation == true;
    /// <summary>Returns a flag indication whether the user attempting to sign-in requires email confirmation .</summary>
    public static bool RequiresEmailConfirmation(this SignInResult result) => (result as ExtendedSigninResult)?.RequiresEmailValidation == true;
    /// <summary>Returns a flag indication whether the user attempting to sign-in requires email confirmation .</summary>
    public static bool RequiresPasswordChange(this SignInResult result) => (result as ExtendedSigninResult)?.RequiresPasswordChange == true;
}

internal sealed class TwoFactorAuthenticationInfo
{
    public string UserId { get; set; }
    public string LoginProvider { get; set; }
}
