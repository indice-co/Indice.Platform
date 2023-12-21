using System.Security.Claims;
using IdentityModel;
using Indice.AspNetCore.Extensions;
using Indice.Events;
using Indice.Features.Identity.Core.Configuration;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Core.Events.Models;
using Indice.Features.Identity.Core.Extensions;
using Indice.Features.Identity.Core.ImpossibleTravel;
using Indice.Features.Identity.Core.PasswordValidation;
using Indice.Features.Identity.Core.Types;
using Indice.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Indice.Features.Identity.Core;

/// <summary>Provides the APIs for user sign in.</summary>
/// <typeparam name="TUser"></typeparam>
public class ExtendedSignInManager<TUser> : SignInManager<TUser> where TUser : User
{
    /// <summary>Default duration in days for two-factor remember.</summary>
    public const int DEFAULT_MFA_REMEMBER_DURATION_IN_DAYS = 90;
    private const string LOGIN_PROVIDER_KEY = "LoginProvider";
    private const string XSRF_KEY = "XsrfId";
    private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;
    private readonly IUserStore<TUser> _userStore;
    private readonly ISignInGuard<TUser> _signInGuard;
    private readonly IPlatformEventService _eventService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserStateProvider<TUser> _stateProvider;

    /// <summary>Creates a new instance of <see cref="SignInManager{TUser}" /></summary>
    /// <param name="userManager">An instance of <see cref="ExtendedUserManager{TUser}"/> used to retrieve users from and persist users.</param>
    /// <param name="httpContextAccessor">The accessor used to access the <see cref="HttpContext"/>.</param>
    /// <param name="claimsFactory">The factory to use to create claims principals for a user.</param>
    /// <param name="optionsAccessor">The accessor used to access the <see cref="IdentityOptions"/>.</param>
    /// <param name="logger">The logger used to log messages, warnings and errors.</param>
    /// <param name="schemes">The scheme provider that is used enumerate the authentication schemes.</param>
    /// <param name="confirmation">The <see cref="IUserConfirmation{TUser}"/> used check whether a user account is confirmed.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="authenticationSchemeProvider">Responsible for managing what authenticationSchemes are supported.</param>
    /// <param name="userStore">Provides an abstraction for a store which manages user accounts.</param>
    /// <param name="userStateProvider">A service used to implement state machine for <see cref="ExtendedUserManager{TUser}"/> and <see cref="ExtendedSignInManager{TUser}"/>.</param>
    /// <param name="signInGuard">Abstracts the process of running various rules that determine whether a login attempt is suspicious or not.</param>
    /// <param name="eventService">Models the event mechanism used to raise events inside the platform.</param>
    public ExtendedSignInManager(
        ExtendedUserManager<TUser> userManager,
        IHttpContextAccessor httpContextAccessor,
        IUserClaimsPrincipalFactory<TUser> claimsFactory,
        IOptionsSnapshot<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<TUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<TUser> confirmation,
        IConfiguration configuration,
        IAuthenticationSchemeProvider authenticationSchemeProvider,
        IUserStore<TUser> userStore,
        IUserStateProvider<TUser> userStateProvider,
        ISignInGuard<TUser> signInGuard,
        IPlatformEventService eventService
    ) : base(userManager, httpContextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation) {
        _authenticationSchemeProvider = authenticationSchemeProvider ?? throw new ArgumentNullException(nameof(authenticationSchemeProvider));
        _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(_httpContextAccessor));
        _stateProvider = userStateProvider ?? throw new ArgumentNullException(nameof(userStateProvider));
        _signInGuard = signInGuard ?? throw new ArgumentNullException(nameof(signInGuard));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        RequirePostSignInConfirmedEmail = configuration.GetIdentityOption<bool?>(nameof(IdentityOptions.SignIn), nameof(RequirePostSignInConfirmedEmail)) == true;
        RequirePostSignInConfirmedPhoneNumber = configuration.GetIdentityOption<bool?>(nameof(IdentityOptions.SignIn), nameof(RequirePostSignInConfirmedPhoneNumber)) == true;
        ExpireBlacklistedPasswordsOnSignIn = configuration.GetIdentityOption<bool?>(nameof(IdentityOptions.SignIn), nameof(ExpireBlacklistedPasswordsOnSignIn)) == true;
        ExternalScheme = configuration.GetIdentityOption<string>(nameof(IdentityOptions.SignIn), nameof(ExternalScheme)) ?? IdentityConstants.ExternalScheme;
        PersistTrustedBrowsers = configuration.GetIdentityOption<bool?>($"{nameof(IdentityOptions.SignIn)}:Mfa", nameof(PersistTrustedBrowsers)) == true;
        MfaRememberDurationInDays = configuration.GetIdentityOption<int?>($"{nameof(IdentityOptions.SignIn)}:Mfa", "RememberDurationInDays") ?? DEFAULT_MFA_REMEMBER_DURATION_IN_DAYS;
        RememberTrustedBrowserAcrossSessions = configuration.GetIdentityOption<bool?>($"{nameof(IdentityOptions.SignIn)}:Mfa", nameof(RememberTrustedBrowserAcrossSessions)) == true;
        RememberExpirationType = configuration.GetIdentityOption<MfaExpirationType?>($"{nameof(IdentityOptions.SignIn)}:Mfa", nameof(RememberExpirationType)) ?? default;
        RequireMfaWhenUserHasTrustedBrowserButExpiredPassword = configuration.GetIdentityOption<bool?>($"{nameof(IdentityOptions.SignIn)}:Mfa:RequireWhen", "UserHasTrustedBrowserButExpiredPassword") ?? true;
    }

    private ExtendedUserManager<TUser> ExtendedUserManager => (ExtendedUserManager<TUser>)UserManager;
    /// <summary>Enables the feature post login email confirmation.</summary>
    public bool RequirePostSignInConfirmedEmail { get; }
    /// <summary>Enables the feature post login phone number confirmation.</summary>
    public bool RequirePostSignInConfirmedPhoneNumber { get; }
    /// <summary>If enabled then users with blacklisted passwords will be forced to change their password upon sign-in instead of waiting for the next time they need to change it.</summary>
    public bool ExpireBlacklistedPasswordsOnSignIn { get; }
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
    /// <summary>Quite self-explanatory property name. Defaults to true.</summary>
    public bool RequireMfaWhenUserHasTrustedBrowserButExpiredPassword { get; }

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
    public override async Task<bool> CanSignInAsync(TUser user) {
        if (user is User && user.Blocked) {
            Logger.LogWarning(0, "User {userId} cannot sign in. User is blocked by the administrator.", await ExtendedUserManager.GetUserIdAsync(user));
            return false;
        }
        return await base.CanSignInAsync(user);
    }

    /// <inheritdoc/>
    protected override async Task<SignInResult> SignInOrTwoFactorAsync(TUser user, bool isPersistent, string loginProvider = null, bool bypassTwoFactor = false) {
        var isExternalLogin = !string.IsNullOrWhiteSpace(loginProvider) && (await _authenticationSchemeProvider.GetExternalSchemesAsync()).Select(scheme => scheme.Name).Contains(loginProvider);
        var deviceId = await _httpContextAccessor.HttpContext.ResolveDeviceId();
        if (isExternalLogin) {
            await _stateProvider.ChangeStateAsync(user, UserAction.ExternalLogin);
            bypassTwoFactor = true;
        } else {
            await _stateProvider.ChangeStateAsync(user, UserAction.Login);
        }
        var result = await _signInGuard.IsSuspiciousLogin(_httpContextAccessor.HttpContext, user);
        if (result.Warning == SignInWarning.ImpossibleTravel && _signInGuard.ImpossibleTravelDetector.FlowType == ImpossibleTravelFlowType.DenyLogin) {
            return SignInResult.Failed;
        }
        if (_stateProvider.ShouldSignInPartially()) {
            return await DoPartialSignInAsync(user, ["pwd"]);
        }
        var mfaImplicitlyPassed = false;
        if (!bypassTwoFactor && await IsTfaEnabled(user)) {
            if (result.Warning == SignInWarning.ImpossibleTravel || !await IsTwoFactorClientRememberedAsync(user)) {
                var userId = await ExtendedUserManager.GetUserIdAsync(user);
                await Context.SignInAsync(IdentityConstants.TwoFactorUserIdScheme, StoreTwoFactorInfo(userId, loginProvider));
                return SignInResult.TwoFactorRequired;
            } else {
                mfaImplicitlyPassed = true;
            }
        }
        if (user is User) {
            var userDevice = deviceId?.Value is not null ? user.Devices?.FirstOrDefault(x => x.DeviceId == deviceId.Value) : null;
            if (userDevice is not null) {
                userDevice.LastSignInDate = DateTimeOffset.UtcNow;
                await ExtendedUserManager.UpdateDeviceAsync(user, userDevice);
            }
            if (RememberExpirationType == MfaExpirationType.Sliding) {
                var authenticateResult = await Context.AuthenticateAsync(IdentityConstants.TwoFactorRememberMeScheme);
                if (authenticateResult.Succeeded && authenticateResult.Principal is not null) {
                    await RememberTwoFactorClientAsync(user);
                }
            }
        }
        if (_stateProvider.ShouldSignInPartially()) {
            var authenticationMethods = new List<string> { "pwd" };
            if (mfaImplicitlyPassed) {
                authenticationMethods.Add("mfa");
            }
            return await DoPartialSignInAsync(user, [.. authenticationMethods]);
        }
        if (loginProvider is null) {
            var additionalClaims = new List<Claim> {
                new(JwtClaimTypes.AuthenticationMethod, "pwd")
            };
            if (!string.IsNullOrWhiteSpace(deviceId?.Value)) {
                additionalClaims.Add(new Claim(BasicClaimTypes.DeviceId, deviceId.Value));
            }
            if (mfaImplicitlyPassed) {
                additionalClaims.Add(new Claim(JwtClaimTypes.AuthenticationMethod, "mfa"));
            }
            await SignInWithClaimsAsync(user, isPersistent, additionalClaims);
        } else {
            await Context.SignOutAsync(IdentityConstants.ExternalScheme);
            await SignInAsync(user, isPersistent, loginProvider);
        }
        return SignInResult.Success;
    }

    /// <inheritdoc/>
    public override async Task SignInWithClaimsAsync(TUser user, AuthenticationProperties authenticationProperties, IEnumerable<Claim> additionalClaims) {
        user.LastSignInDate = DateTimeOffset.UtcNow;
        await ExtendedUserManager.UpdateAsync(user);
        await base.SignInWithClaimsAsync(user, authenticationProperties, additionalClaims);
        var result = await _signInGuard.IsSuspiciousLogin(_httpContextAccessor.HttpContext, user);
        await _eventService.Publish(UserLoginEvent.Success(
            UserEventContext.InitializeFromUser(user),
            result.Warning,
            additionalClaims.Where(claim => claim.Type == JwtClaimTypes.AuthenticationMethod).Select(claim => claim.Value).ToArray()
        ));
    }

    /// <inheritdoc/>
    public override async Task<SignInResult> TwoFactorSignInAsync(string provider, string code, bool isPersistent, bool rememberClient) {
        var twoFactorInfo = await RetrieveTwoFactorInfoAsync();
        if (twoFactorInfo == null || twoFactorInfo.UserId == null) {
            return SignInResult.Failed;
        }
        var user = await ExtendedUserManager.FindByIdAsync(twoFactorInfo.UserId);
        if (user == null) {
            return SignInResult.Failed;
        }
        var error = await PreSignInCheck(user);
        if (error != null) {
            return error;
        }
        if (await ExtendedUserManager.VerifyTwoFactorTokenAsync(user, provider, code)) {
            await _stateProvider.ChangeStateAsync(user, UserAction.MultiFactorAuthenticated);
            await DoTwoFactorSignInAsync(user, twoFactorInfo, isPersistent, rememberClient);
            if (_stateProvider.ShouldSignInPartially()) {
                return await DoPartialSignInAsync(user, new string[] { "pwd", "mfa" });
            }
            return new ExtendedSignInResult(_stateProvider.CurrentState);
        }
        if (ExtendedUserManager.SupportsUserLockout) {
            await ExtendedUserManager.AccessFailedAsync(user);
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
        var allSchemes = await _authenticationSchemeProvider.GetAllSchemesAsync();
        // Check if authentication scheme is registered before trying to sign out, to avoid errors.
        var schemes = new string[] {
            ExtendedIdentityConstants.ExtendedValidationUserIdScheme,
            ExtendedIdentityConstants.MfaOnboardingScheme,
            ExternalScheme
        };
        foreach (var scheme in schemes) {
            if (allSchemes.FirstOrDefault(x => x.Name == scheme) is not null) {
                await Context.SignOutAsync(scheme);
            }
        }
        await base.SignOutAsync();
        _stateProvider.ClearState();
    }

    /// <inheritdoc/>
    public override async Task<SignInResult> CheckPasswordSignInAsync(TUser user, string password, bool lockoutOnFailure) {
        var attempt = await base.CheckPasswordSignInAsync(user, password, lockoutOnFailure);
        if (!attempt.Succeeded) {
            return attempt;
        }
        var result = await _signInGuard.IsSuspiciousLogin(_httpContextAccessor.HttpContext, user);
        await _eventService.Publish(UserPasswordLoginEvent.Success(UserEventContext.InitializeFromUser(user), result.Warning));
        if (ExpireBlacklistedPasswordsOnSignIn) {
            var blacklistPasswordValidator = ExtendedUserManager.PasswordValidators.OfType<NonCommonPasswordValidator<TUser>>().FirstOrDefault();
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
        var userId = await ExtendedUserManager.GetUserIdAsync(user);
        var result = await Context.AuthenticateAsync(IdentityConstants.TwoFactorRememberMeScheme);
        var isRemembered = result?.Principal is not null && result.Principal.FindFirstValue(JwtClaimTypes.Name) == userId;
        var deviceId = await _httpContextAccessor.HttpContext.ResolveDeviceId();
        if (!string.IsNullOrWhiteSpace(deviceId.Value) && (isRemembered || (!isRemembered && RememberTrustedBrowserAcrossSessions))) {
            var device = await ExtendedUserManager.GetDeviceByIdAsync(user, deviceId.Value);
            isRemembered = device is not null &&
                           device.MfaSessionExpirationDate.HasValue &&
                           device.MfaSessionExpirationDate.Value > DateTimeOffset.UtcNow;
            if (RequireMfaWhenUserHasTrustedBrowserButExpiredPassword) {
                isRemembered = isRemembered && !user.HasExpiredPassword();
            }
        }
        if (isRemembered) {
            await _stateProvider.ChangeStateAsync(user, UserAction.MultiFactorAuthenticated);
        }
        return isRemembered;
    }
    #endregion

    #region Custom Methods
    /// <summary>Revokes all sessions for user browsers.</summary>
    /// <param name="user">The user instance.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public Task<IdentityResult> RevokeMfaSessionsAsync(TUser user) {
        if (user is null) {
            throw new ArgumentNullException(nameof(user));
        }
        var deviceStore = GetDeviceStore();
        return deviceStore.SetBrowsersMfaSessionExpirationDate(user, null);
    }

    /// <summary>Automatically signs in the given user.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="scheme">Authenticates the current request using the specified scheme.</param>
    public async Task<AuthenticationProperties> AutoSignIn(TUser user, string scheme) {
        var authenticateResult = await _httpContextAccessor.HttpContext.AuthenticateAsync(scheme);
        AuthenticationProperties authenticationProperties = default;
        if (authenticateResult.Succeeded) {
            authenticationProperties = authenticateResult.Properties;
            await SignInWithClaimsAsync(user, authenticationProperties, authenticateResult.Principal.Claims);
        }
        return authenticationProperties;
    }
    #endregion

    #region Helper Methods
    /// <summary>Performs a partial sign in for the user based on his state.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="authenticationMethods">The authentication methods used during login.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<ExtendedSignInResult> DoPartialSignInAsync(TUser user, string[] authenticationMethods) {
        var scheme = _stateProvider.ShouldSignInForExtendedValidation()
            ? ExtendedIdentityConstants.ExtendedValidationUserIdScheme
            : _stateProvider.ShouldSignInForMfaOnboarding()
                ? ExtendedIdentityConstants.MfaOnboardingScheme
                : throw new InvalidOperationException("Cannot partially sign in.");
        var userClaims = await ExtendedUserManager.GetClaimsAsync(user);
        var firstName = userClaims.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value;
        var lastName = userClaims.FirstOrDefault(x => x.Type == JwtClaimTypes.FamilyName)?.Value;
        var isEmailConfirmed = await ExtendedUserManager.IsEmailConfirmedAsync(user);
        var isPhoneConfirmed = await ExtendedUserManager.IsPhoneNumberConfirmedAsync(user);
        var isPasswordExpired = user.HasExpiredPassword();
        var userId = await ExtendedUserManager.GetUserIdAsync(user);
        var returnUrl = Context.Request.Query["ReturnUrl"];
        await Context.SignInAsync(scheme, StoreValidationInfo(userId, isEmailConfirmed, isPhoneConfirmed, isPasswordExpired, firstName, lastName, user.UserName, authenticationMethods), new AuthenticationProperties {
            RedirectUri = returnUrl,
            IsPersistent = false
        });
        return new ExtendedSignInResult(_stateProvider.CurrentState);
    }

    private async Task<bool> IsTfaEnabled(TUser user)
        => ExtendedUserManager.SupportsUserTwoFactor && await ExtendedUserManager.GetTwoFactorEnabledAsync(user) && (await ExtendedUserManager.GetValidTwoFactorProvidersAsync(user)).Count > 0;

    private static ClaimsPrincipal StoreValidationInfo(string userId, bool isEmailConfirmed, bool isPhoneConfirmed, bool isPasswordExpired, string firstName, string lastName, string userName, string[] authenticationMethods) {
        var identity = new ClaimsIdentity(ExtendedIdentityConstants.ExtendedValidationUserIdScheme);
        identity.AddClaim(new Claim(JwtClaimTypes.Subject, userId));
        identity.AddClaim(new Claim(JwtClaimTypes.EmailVerified, isEmailConfirmed.ToString().ToLower()));
        identity.AddClaim(new Claim(JwtClaimTypes.PhoneNumberVerified, isPhoneConfirmed.ToString().ToLower()));
        identity.AddClaim(new Claim(ExtendedIdentityConstants.PasswordExpiredClaimType, isPasswordExpired.ToString().ToLower()));
        identity.AddClaim(new Claim(JwtClaimTypes.Name, userName));
        if (!string.IsNullOrWhiteSpace(firstName)) {
            identity.AddClaim(new Claim(JwtClaimTypes.GivenName, firstName));
        }
        if (!string.IsNullOrWhiteSpace(lastName)) {
            identity.AddClaim(new Claim(JwtClaimTypes.FamilyName, lastName));
        }
        foreach (var method in authenticationMethods) {
            identity.AddClaim(new Claim(JwtClaimTypes.AuthenticationMethod, method));
        }
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal StoreTwoFactorInfo(string userId, string loginProvider) {
        var identity = new ClaimsIdentity(IdentityConstants.TwoFactorUserIdScheme);
        identity.AddClaim(new Claim(ClaimTypes.Name, userId));
        identity.AddClaim(new Claim(JwtClaimTypes.AuthenticationMethod, "pwd"));
        if (loginProvider != null) {
            identity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, loginProvider));
        }
        return new ClaimsPrincipal(identity);
    }

    private async Task<ClaimsPrincipal> StoreRememberClient(TUser user) {
        var deviceId = await _httpContextAccessor.HttpContext.ResolveDeviceId();
        if (PersistTrustedBrowsers && !string.IsNullOrWhiteSpace(deviceId.Value)) {
            var device = await ExtendedUserManager.GetDeviceByIdAsync(user, deviceId.Value);
            if (device is not null) {
                device.IsTrusted = true;
                device.TrustActivationDate ??= DateTimeOffset.UtcNow;
                device.MfaSessionExpirationDate = DateTimeOffset.UtcNow.AddDays(MfaRememberDurationInDays);
                await ExtendedUserManager.UpdateDeviceAsync(user, device);
            } else {
                var userAgentHeader = Context.Request.Headers[HeaderNames.UserAgent];
                var userAgent = new UserAgent(userAgentHeader);
                var now = DateTimeOffset.UtcNow;
                device = new UserDevice {
                    ClientType = DeviceClientType.Browser,
                    DateCreated = now,
                    DeviceId = deviceId.Value,
                    IsTrusted = true,
                    LastSignInDate = now,
                    MfaSessionExpirationDate = now.AddDays(90),
                    Model = userAgent.DeviceModel,
                    Name = userAgent.DisplayName,
                    OsVersion = userAgent.Os,
                    Platform = userAgent.DevicePlatform,
                    TrustActivationDate = now,
                    User = user,
                    UserId = user.Id
                };
                await ExtendedUserManager.CreateDeviceAsync(user, device);
            }
        }
        var userId = await ExtendedUserManager.GetUserIdAsync(user);
        var deviceIdentity = new ClaimsIdentity(IdentityConstants.TwoFactorRememberMeScheme);
        deviceIdentity.AddClaim(new Claim(JwtClaimTypes.Name, userId));
        if (ExtendedUserManager.SupportsUserSecurityStamp) {
            var stamp = await ExtendedUserManager.GetSecurityStampAsync(user);
            deviceIdentity.AddClaim(new Claim(Options.ClaimsIdentity.SecurityStampClaimType, stamp));
        }
        return new ClaimsPrincipal(deviceIdentity);
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
        if (rememberClient) {
            await RememberTwoFactorClientAsync(user);
        }
        if (_stateProvider.CurrentState != UserState.LoggedIn) {
            return;
        }
        await ResetLockout(user);
        var claims = new List<Claim> {
            new Claim(JwtClaimTypes.AuthenticationMethod, "pwd"),
            new Claim(JwtClaimTypes.AuthenticationMethod, "mfa")
        };
        if (twoFactorInfo.LoginProvider is not null) {
            claims.Add(new Claim(ClaimTypes.AuthenticationMethod, twoFactorInfo.LoginProvider));
            await Context.SignOutAsync(IdentityConstants.ExternalScheme);
        }
        await Context.SignOutAsync(ExtendedIdentityConstants.TwoFactorUserIdScheme);
        await SignInWithClaimsAsync(user, isPersistent, claims);
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
public class ExtendedSignInResult : SignInResult
{
    /// <summary>Constructs an instance of <see cref="ExtendedSignInResult"/>.</summary>
    public ExtendedSignInResult(
        bool requiresEmailVerification,
        bool requiresPhoneNumberVerification,
        bool requiresPasswordChange,
        bool requiresMfaOnboarding,
        bool isImpossibleTraveler) {
        RequiresEmailVerification = requiresEmailVerification;
        RequiresPhoneNumberVerification = requiresPhoneNumberVerification;
        RequiresPasswordChange = requiresPasswordChange;
        RequiresMfaOnboarding = requiresMfaOnboarding;
        IsImpossibleTraveler = isImpossibleTraveler;
    }

    /// <summary>Constructs an instance of <see cref="ExtendedSignInResult"/> based on <see cref="UserState"/>.</summary>
    public ExtendedSignInResult(UserState state) : this(
        state == UserState.RequiresEmailVerification,
        state == UserState.RequiresPhoneNumberVerification,
        state == UserState.RequiresPasswordChange,
        state == UserState.RequiresMfaOnboarding,
        state == UserState.IsImpossibleTraveler) {
        Succeeded = state == UserState.LoggedIn;
    }

    /// <summary>Returns a flag indication whether the user attempting to sign-in requires phone number confirmation.</summary>
    /// <value>True if the user attempting to sign-in requires phone number confirmation, otherwise false.</value>
    public bool RequiresPhoneNumberVerification { get; }
    /// <summary>Returns a flag indication whether the user attempting to sign-in requires email confirmation.</summary>
    /// <value>True if the user attempting to sign-in requires email confirmation, otherwise false.</value>
    public bool RequiresEmailVerification { get; }
    /// <summary>Returns a flag indication whether the user attempting to sign-in requires an immediate password change due to expiration.</summary>
    /// <value>True if the user attempting to sign-in requires a password change, otherwise false.</value>
    public bool RequiresPasswordChange { get; }
    /// <summary>Returns a flag indication whether the user should on-board to MFA.</summary>
    /// <value>True if the user attempting to sign-in requires MFA on-boarding, otherwise false.</value>
    public bool RequiresMfaOnboarding { get; }
    /// <summary>Returns a flag indication whether the user should be treated with suspicion.</summary>
    /// <value>True if the user attempting to sign-in from a galaxy far far away.</value>
    public bool IsImpossibleTraveler { get; }
}

/// <summary>Extensions on <see cref="SignInResult"/> type.</summary>
public static class ExtendedSignInManagerExtensions
{
    /// <summary>Returns a flag indication whether the user attempting to sign-in requires phone number confirmation.</summary>
    /// <param name="result">Represents the result of a sign-in operation.</param>
    public static bool RequiresPhoneNumberConfirmation(this SignInResult result) => result is ExtendedSignInResult { RequiresPhoneNumberVerification: true };
    /// <summary>Returns a flag indication whether the user attempting to sign-in requires email confirmation .</summary>
    public static bool RequiresEmailConfirmation(this SignInResult result) => result is ExtendedSignInResult { RequiresEmailVerification: true };
    /// <summary>Returns a flag indication whether the user attempting to sign-in requires email confirmation .</summary>
    public static bool RequiresPasswordChange(this SignInResult result) => result is ExtendedSignInResult { RequiresPasswordChange: true };
    /// <summary>Returns a flag indication whether the user should on-board to MFA.</summary>
    public static bool RequiresMfaOnboarding(this SignInResult result) => result is ExtendedSignInResult { RequiresMfaOnboarding: true };
    /// <summary>Returns a flag indication whether the user is an impossible traveler.</summary>
    public static bool IsImpossibleTraveler(this SignInResult result) => result is ExtendedSignInResult { IsImpossibleTraveler: true };
}

internal sealed class TwoFactorAuthenticationInfo
{
    public string UserId { get; set; }
    public string LoginProvider { get; set; }
}
