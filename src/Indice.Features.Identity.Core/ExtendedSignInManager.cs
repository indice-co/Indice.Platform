using System.Globalization;
using System.Linq;
using System.Security.Claims;
#if NET9_0_OR_GREATER
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Extensions;
#else
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Extensions;
#endif
using Indice.Events;
using Indice.Features.Identity.Core.Configuration;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Data.Stores;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Core.Events.Models;
using Indice.Features.Identity.Core.Extensions;
using Indice.Features.Identity.Core.ImpossibleTravel;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.Core.PasswordValidation;
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
    private readonly IUserRequirementProvider<TUser> _userRequirementProvider;

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
    /// <param name="signInGuard">Abstracts the process of running various rules that determine whether a login attempt is suspicious or not.</param>
    /// <param name="eventService">Models the event mechanism used to raise events inside the platform.</param>
    /// <param name="userRequirementProvider">Provides information about the user active validation requirements.</param>
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
        ISignInGuard<TUser> signInGuard,
        IPlatformEventService eventService,
        IUserRequirementProvider<TUser> userRequirementProvider
    ) : base(userManager, httpContextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation) {
        _authenticationSchemeProvider = authenticationSchemeProvider ?? throw new ArgumentNullException(nameof(authenticationSchemeProvider));
        _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
        _signInGuard = signInGuard ?? throw new ArgumentNullException(nameof(signInGuard));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _userRequirementProvider = userRequirementProvider ?? throw new ArgumentNullException(nameof(userRequirementProvider));
        RequirePostSignInConfirmedEmail = configuration.GetIdentityOption<bool>(nameof(IdentityOptions.SignIn), nameof(RequirePostSignInConfirmedEmail));
        RequirePostSignInConfirmedPhoneNumber = configuration.GetIdentityOption<bool>(nameof(IdentityOptions.SignIn), nameof(RequirePostSignInConfirmedPhoneNumber));
        RequirePostSignInAcceptedTerms = configuration.GetIdentityOption<bool>(nameof(IdentityOptions.SignIn), nameof(RequirePostSignInAcceptedTerms));
        ExpireBlacklistedPasswordsOnSignIn = configuration.GetIdentityOption<bool>(nameof(IdentityOptions.SignIn), nameof(ExpireBlacklistedPasswordsOnSignIn));
        PersistTrustedBrowsers = configuration.GetIdentityOption<bool?>($"{nameof(IdentityOptions.SignIn)}:Mfa", nameof(PersistTrustedBrowsers)) ?? true;
        MfaRememberDurationInDays = configuration.GetIdentityOption<int?>($"{nameof(IdentityOptions.SignIn)}:Mfa", "RememberDurationInDays") ?? DEFAULT_MFA_REMEMBER_DURATION_IN_DAYS;
        RememberTrustedBrowserAcrossSessions = configuration.GetIdentityOption<bool?>($"{nameof(IdentityOptions.SignIn)}:Mfa", nameof(RememberTrustedBrowserAcrossSessions)) ?? true;
        RememberExpirationType = configuration.GetIdentityOption<MfaExpirationType>($"{nameof(IdentityOptions.SignIn)}:Mfa", nameof(RememberExpirationType));
        RequireMfaWhenUserHasTrustedBrowserButExpiredPassword = configuration.GetIdentityOption<bool?>($"{nameof(IdentityOptions.SignIn)}:Mfa:RequireWhen", "UserHasTrustedBrowserButExpiredPassword") ?? true;
        MfaPolicy = configuration.GetIdentityOption<MfaPolicy?>($"{nameof(IdentityOptions.SignIn)}:Mfa", "Policy") ?? MfaPolicy.Optional;
        var latestTermsRelease = configuration.GetIdentityOption<string?>(nameof(IdentityOptions.SignIn), nameof(ExtendedSignInManager<User>.LatestTermsReleaseDate));
        var latestTermsReleaseDate = DateTime.MinValue;
        if (!string.IsNullOrEmpty(latestTermsRelease))
            DateTime.TryParseExact(latestTermsRelease, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out latestTermsReleaseDate);
        LatestTermsReleaseDate = latestTermsReleaseDate;
    }

    private ExtendedUserManager<TUser> ExtendedUserManager => (ExtendedUserManager<TUser>)UserManager;
    /// <summary>Enables the feature post login email confirmation.</summary>
    public bool RequirePostSignInConfirmedEmail { get; }
    /// <summary>Enables the feature post login phone number confirmation.</summary>
    public bool RequirePostSignInConfirmedPhoneNumber { get; }
    /// <summary>Enables the feature post login terms acceptance.</summary>
    public bool RequirePostSignInAcceptedTerms { get; }
    /// <summary>Gets the date when the new terms were released.</summary>
    public DateTime? LatestTermsReleaseDate { get; }
    /// <summary>If enabled then users with blacklisted passwords will be forced to change their password upon sign-in instead of waiting for the next time they need to change it.</summary>
    public bool ExpireBlacklistedPasswordsOnSignIn { get; }
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
    /// <summary>MFA policy applied for new users.</summary>
    public MfaPolicy MfaPolicy { get; }

    #region Method Overrides
    /// <inheritdoc/>
    public override async Task<ExternalLoginInfo?> GetExternalLoginInfoAsync(string? expectedXsrf = null) {
        var auth = await Context.AuthenticateAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);
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
            AuthenticationTokens = auth.Properties!.GetTokens()
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
    protected override async Task<SignInResult> SignInOrTwoFactorAsync(TUser user, bool isPersistent, string? loginProvider = null, bool bypassTwoFactor = false) {
        var deviceId = await GetMfaDeviceIdentifierAsync(user);
        
        var result = await _signInGuard.IsSuspiciousLogin(Context!, user);
        if (result.Warning == SignInWarning.ImpossibleTravel && _signInGuard.ImpossibleTravelDetector?.FlowType == ImpossibleTravelFlowType.DenyLogin) {
            return SignInResult.Failed;
        }

        var mfaImplicitlyPassed = false;
        if (!bypassTwoFactor && await IsTfaEnabled(user)) {
            if (result.Warning == SignInWarning.ImpossibleTravel || !await IsTwoFactorClientRememberedAsync(user)) {
                var userId = await ExtendedUserManager.GetUserIdAsync(user);
                await Context.SignInAsync(IdentityConstants.TwoFactorUserIdScheme, ClaimsPrincipalFromTwoFactorInfo(userId, deviceId, loginProvider));
                return SignInResult.TwoFactorRequired;
            }
            mfaImplicitlyPassed = true;
        }

        var userDevice = !deviceId.IsEmpty ? user.Devices?.FirstOrDefault(x => x.DeviceId == deviceId.Value) : null;
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

        List<string> authenticationMethods = [loginProvider ?? "pwd"];
        if (mfaImplicitlyPassed) {
            authenticationMethods.Add("mfa");
        }
        if (await ShouldSignInForExtendedValidationAsync(user)) {
            return await DoPartialSignInAsync(user, deviceId, [.. authenticationMethods]);
        }
        if (loginProvider != null) {
            // Cleanup external cookie
            await Context.SignOutAsync(IdentityConstants.ExternalScheme);
            await Context.SignOutAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);   
        }
        List<Claim> additionalClaims = [.. authenticationMethods.Select(amr => new Claim(JwtClaimTypes.AuthenticationMethod, amr))];
        if (!deviceId.IsEmpty) {
            additionalClaims.Add(new (BasicClaimTypes.DeviceId, deviceId.Value!));
        }
        await SignInWithClaimsAsync(user, isPersistent, additionalClaims);
        return SignInResult.Success;
    }

    /// <inheritdoc/>
    public override async Task SignInWithClaimsAsync(TUser user, AuthenticationProperties? authenticationProperties, IEnumerable<Claim> additionalClaims) {
        user.LastSignInDate = DateTimeOffset.UtcNow;

        var amr = additionalClaims.Where(claim => claim.Type == JwtClaimTypes.AuthenticationMethod).Select(claim => claim.Value).ToArray();
        var federatedLoginProvider = amr.Where(x => !new[] { "pwd", "mfa" }.Contains(x)).Select(x => new Claim(JwtClaimTypes.IdentityProvider, x)).FirstOrDefault();
        additionalClaims = federatedLoginProvider != null ? [federatedLoginProvider, ..additionalClaims] : additionalClaims;
        await ExtendedUserManager.UpdateAsync(user);
        await base.SignInWithClaimsAsync(user, authenticationProperties, additionalClaims);
        var result = await _signInGuard.IsSuspiciousLogin(Context, user);
        await _eventService.Publish(UserLoginEvent.Success(
            UserEventContext.InitializeFromUser(user),
            authenticationProperties.GetSessionId(),
            result.Warning,
            federatedLoginProvider?.Value,
            amr
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
            return error!;
        }
        if (await ExtendedUserManager.VerifyTwoFactorTokenAsync(user, provider, code)) {
            return await DoTwoFactorSignInAsync(user, twoFactorInfo, isPersistent, rememberClient);
        }
        if (ExtendedUserManager.SupportsUserLockout) {
            await ExtendedUserManager.AccessFailedAsync(user);
        }
        return SignInResult.Failed;
    }

    /// <inheritdoc/>
    public override async Task<TUser?> GetTwoFactorAuthenticationUserAsync() {
        var info = await RetrieveTwoFactorInfoAsync();
        if (string.IsNullOrWhiteSpace(info?.UserId)) {
            return default;
        }
        return await ExtendedUserManager.FindByIdAsync(info.UserId!);
    }

    /// <inheritdoc/>
    public async override Task SignOutAsync() {
        var allSchemes = await _authenticationSchemeProvider.GetAllSchemesAsync();
        // Check if authentication scheme is registered before trying to sign out, to avoid errors.
        var schemes = new string[] {
            ExtendedIdentityConstants.ExtendedValidationScheme,
            IdentityServerConstants.ExternalCookieAuthenticationScheme
        };
        foreach (var scheme in schemes) {
            if (allSchemes.FirstOrDefault(x => x.Name == scheme) is not null) {
                await Context.SignOutAsync(scheme);
            }
        }
        await base.SignOutAsync();
    }

    /// <inheritdoc/>
    public override async Task<SignInResult> CheckPasswordSignInAsync(TUser user, string password, bool lockoutOnFailure) {
        var attempt = await base.CheckPasswordSignInAsync(user, password, lockoutOnFailure);
        if (!attempt.Succeeded) {
            return attempt;
        }
        var result = await _signInGuard.IsSuspiciousLogin(Context, user);
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
    public override AuthenticationProperties ConfigureExternalAuthenticationProperties(string? provider, string? redirectUrl, string? userId = null) {
        var props = base.ConfigureExternalAuthenticationProperties(provider, redirectUrl, userId);
        var queryString = QueryHelpers.ParseNullableQuery(redirectUrl);
        // Make available the 'prompt' parameter to the downstream identity provider so that the client can have control over the re-authentication process.
        // This merely adds the item to the authentication properties.
        // The next thing to do is to configure the OpenIdConnect middleware to pass it on.
        if (queryString is not null && queryString.ContainsKey("prompt")) {
            props.Items.Add("prompt", queryString["prompt"]);
        }
        return props;
    }

    /// <inheritdoc/>
    public override async Task RememberTwoFactorClientAsync(TUser user) {
        var deviceId = await GetMfaDeviceIdentifierAsync(user);
        var principal = await StoreRememberClient(user, deviceId);
        await Context.SignInAsync(IdentityConstants.TwoFactorRememberMeScheme, principal, new AuthenticationProperties { IsPersistent = true });
    }

    /// <inheritdoc/>
    public override async Task<bool> IsTwoFactorClientRememberedAsync(TUser user) {
        var userId = await ExtendedUserManager.GetUserIdAsync(user);
        var deviceId = await GetMfaDeviceIdentifierAsync(user);
        if (!deviceId.IsEmpty) {
            var device = await ExtendedUserManager.GetDeviceByIdAsync(user, deviceId.Value!);
            if (device == null) {
                return false;
            }
            if (RequireMfaWhenUserHasTrustedBrowserButExpiredPassword && user.HasExpiredPassword()) {
                return false;
            }
            var isRemembered = device.MfaSessionActive();
            return isRemembered;
        }
        return false;
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
        return deviceStore!.SetBrowsersMfaSessionExpirationDate(user, expirationDate: null);
    }

    /// <summary>Automatically signs in the given user.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="scheme">Authenticates the current request using the specified scheme.</param>
    public async Task<AuthenticationProperties?> AutoSignIn(TUser user, string scheme) {
        var authenticateResult = await Context!.AuthenticateAsync(scheme);
        AuthenticationProperties? authenticationProperties = default;
        if (authenticateResult.Succeeded) {
            authenticationProperties = authenticateResult.Properties;
            await SignInWithClaimsAsync(user, authenticationProperties, authenticateResult.Principal.Claims.Where(x => x.Type == JwtClaimTypes.AuthenticationMethod || x.Type == BasicClaimTypes.DeviceId));
            await Context!.SignOutAsync(scheme);
        }
        return authenticationProperties;
    }

    /// <summary>Gets the current device id from context (broser cookie or form post)</summary>
    /// <param name="user"></param>
    /// <returns>The device identifier</returns>
    public async Task<MfaDeviceIdentifier> GetMfaDeviceIdentifierAsync(TUser user) {
        var result = await Context.AuthenticateAsync(IdentityConstants.TwoFactorRememberMeScheme);
        if (!result.Succeeded || result.Principal?.FindSubjectId() != user.Id) {
            return Context.ResolveDeviceId();
        }
        return new MfaDeviceIdentifier(result.Principal.FindFirstValue(BasicClaimTypes.DeviceId));
    }
    #endregion

    #region Helper Methods


    /// <summary>Performs a partial sign in for the user based on his state.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="deviceId">The device id that represents the current client (browser)</param>
    /// <param name="authenticationMethods">The authentication methods used during login.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<SignInResult> DoPartialSignInAsync(TUser user, MfaDeviceIdentifier deviceId, string[] authenticationMethods) {
        var userClaims = await ExtendedUserManager.GetClaimsAsync(user);
        var firstName = userClaims.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value;
        var lastName = userClaims.FirstOrDefault(x => x.Type == JwtClaimTypes.FamilyName)?.Value;
        var isEmailConfirmed = user.EmailConfirmed;
        var isPhoneConfirmed = user.PhoneNumberConfirmed;
        var isPasswordExpired = user.HasExpiredPassword();

        var result = ExtendedSignInResult.ValidationRequired;
        var userId = user.Id;
        var returnUrl = Context.Request.Query["ReturnUrl"];

        await Context.SignInAsync(ExtendedIdentityConstants.ExtendedValidationScheme, ClaimsPrincipalFromValidationInfo(userId, deviceId, isEmailConfirmed, isPhoneConfirmed, isPasswordExpired, firstName, lastName, user.UserName!, authenticationMethods), new AuthenticationProperties {
            RedirectUri = returnUrl,
            IsPersistent = false
        });
        return result;
    }

    /// <summary>
    /// Determines whether the user should be signed in for extended validation.
    /// </summary>
    /// <param name="user">The user to check</param>
    /// <returns>True in case of extended validaton requirement</returns>
    public Task<bool> ShouldSignInForExtendedValidationAsync(TUser user) => 
        _userRequirementProvider.RequiresValidationAsync(Context!, user);

    private async Task<bool> IsTfaEnabled(TUser user)
        => ExtendedUserManager.SupportsUserTwoFactor && user.TwoFactorEnabled && (await ExtendedUserManager.GetValidTwoFactorProvidersAsync(user)).Count > 0;

    private static ClaimsPrincipal ClaimsPrincipalFromValidationInfo(string userId, MfaDeviceIdentifier deviceId, bool isEmailConfirmed, bool isPhoneConfirmed, bool isPasswordExpired, string? firstName, string? lastName, string userName, string[] authenticationMethods) {
        var identity = new ClaimsIdentity(ExtendedIdentityConstants.ExtendedValidationScheme);
        identity.AddClaim(new Claim(JwtClaimTypes.Subject, userId));
        identity.AddClaim(new Claim(JwtClaimTypes.EmailVerified, isEmailConfirmed.ToString().ToLower()));
        identity.AddClaim(new Claim(JwtClaimTypes.PhoneNumberVerified, isPhoneConfirmed.ToString().ToLower()));
        identity.AddClaim(new Claim(ExtendedIdentityConstants.PasswordExpiredClaimType, isPasswordExpired.ToString().ToLower()));
        identity.AddClaim(new Claim(JwtClaimTypes.Name, userName));
        if (!deviceId.IsEmpty) {
            identity.AddClaim(new Claim(BasicClaimTypes.DeviceId, deviceId.Value!));
        }
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

    private ClaimsPrincipal ClaimsPrincipalFromTwoFactorInfo(string userId, MfaDeviceIdentifier deviceId, string? loginProvider) {
        var identity = new ClaimsIdentity(IdentityConstants.TwoFactorUserIdScheme);
        identity.AddClaim(new Claim(Options.ClaimsIdentity.UserIdClaimType, userId));
        identity.AddClaim(new Claim(JwtClaimTypes.AuthenticationMethod, loginProvider ?? "pwd"));
        if (!deviceId.IsEmpty) { 
            identity.AddClaim(new Claim(BasicClaimTypes.DeviceId, deviceId.Value!));
        }
        return new ClaimsPrincipal(identity);
    }

    private async Task<ClaimsPrincipal> StoreRememberClient(TUser user, MfaDeviceIdentifier deviceId) {
        if (PersistTrustedBrowsers && !string.IsNullOrWhiteSpace(deviceId.Value)) {
            var device = await ExtendedUserManager.GetDeviceByIdAsync(user, deviceId.Value);
            if (device is not null) {
                device.RenewTrust(MfaRememberDurationInDays);
                await ExtendedUserManager.UpdateDeviceAsync(user, device);
            } else {
                var userAgentHeader = Context.Request.Headers[HeaderNames.UserAgent];
                device = UserDevice.FromUserAgent(userAgentHeader!, deviceId, user.Id, MfaRememberDurationInDays);
                device.User = user;
                await ExtendedUserManager.CreateDeviceAsync(user, device);
            }
        }
        var userId = await ExtendedUserManager.GetUserIdAsync(user);
        var deviceIdentity = new ClaimsIdentity(IdentityConstants.TwoFactorRememberMeScheme);
        deviceIdentity.AddClaim(new Claim(Options.ClaimsIdentity.UserIdClaimType, userId));
        if (!deviceId.IsEmpty) { 
            deviceIdentity.AddClaim(new Claim(BasicClaimTypes.DeviceId, deviceId.Value!));
        }
        if (ExtendedUserManager.SupportsUserSecurityStamp) {
            deviceIdentity.AddClaim(new Claim(Options.ClaimsIdentity.SecurityStampClaimType, user.SecurityStamp ?? string.Empty));
        }
        return new ClaimsPrincipal(deviceIdentity);
    }

    private async Task<TwoFactorAuthenticationInfo?> RetrieveTwoFactorInfoAsync() {
        var result = await Context.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme);
        var claimsPrincipal = result?.Principal;
        if (claimsPrincipal is null) {
            return default;
        }
        var userId = claimsPrincipal.FindFirstValue(Options.ClaimsIdentity.UserIdClaimType) ??
                     claimsPrincipal.FindFirstValue(JwtClaimTypes.Subject) ??
                     claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        var deviceId = new MfaDeviceIdentifier(claimsPrincipal.FindFirstValue(BasicClaimTypes.DeviceId));
        var authenticationMethod = claimsPrincipal.FindFirstValue(JwtClaimTypes.AuthenticationMethod) ??
                                   claimsPrincipal.FindFirstValue(ClaimTypes.AuthenticationMethod);
        return new TwoFactorAuthenticationInfo {
            UserId = userId!,
            DeviceId = deviceId,
            LoginProvider = authenticationMethod!
        };
    }

    private async Task<SignInResult> DoTwoFactorSignInAsync(TUser user, TwoFactorAuthenticationInfo twoFactorInfo, bool isPersistent, bool rememberClient) {
        if (rememberClient) {
            await RememberTwoFactorClientAsync(user);
        }
        await ResetLockout(user);
        List<Claim> claims = [ 
            new(JwtClaimTypes.AuthenticationMethod, twoFactorInfo.LoginProvider ?? "pwd"), 
            new(JwtClaimTypes.AuthenticationMethod, "mfa") 
        ];
        if (twoFactorInfo.LoginProvider is not null) {
            await Context.SignOutAsync(IdentityConstants.ExternalScheme);
            await Context.SignOutAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);
        }
        if (!twoFactorInfo.DeviceId.IsEmpty) {
            claims.Add(new Claim(BasicClaimTypes.DeviceId, twoFactorInfo.DeviceId.Value!));
        }
        await Context.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
        if (await ShouldSignInForExtendedValidationAsync(user)) {
            return await DoPartialSignInAsync(user, twoFactorInfo.DeviceId, [twoFactorInfo.LoginProvider ?? "pwd", "mfa"]);
        }
        await SignInWithClaimsAsync(user, isPersistent, claims);
        return SignInResult.Success;
    }

    private IUserDeviceStore<TUser>? GetDeviceStore(bool throwOnFail = true) {
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
    private static readonly SignInResult _validationRequired = new ExtendedSignInResult { RequiresValidation = true };
    /// <summary>Constructs an instance of <see cref="ExtendedSignInResult"/>.</summary>
    public ExtendedSignInResult() {
    }

    /// <summary>Returns a flag indication whether the user attempting to sign-in requires further validation for example password change, email verification, phone number verification, or mfa onboarding before moving to signin.</summary>
    /// <value>True if the user attempting to sign-in requires validation, otherwise false.</value>
    public bool RequiresValidation { get; protected set; }

    /// <summary>
    /// Returns a <see cref="SignInResult"/> that represents a sign-in attempt that needs user validation.
    /// </summary>
    /// <returns>A <see cref="SignInResult"/> that represents sign-in attempt that needs user validation.</returns>
    public static SignInResult ValidationRequired => _validationRequired;

    /// <inheritdoc/>
    public override string ToString() => RequiresValidation ? "RequiresValidation" : base.ToString();
}

/// <summary>Extensions on <see cref="SignInResult"/> type.</summary>
public static class ExtendedSignInManagerExtensions
{
    /// <summary>Returns a flag indication whether the user attempting to sign-in requires extended validation.</summary>
    public static bool RequiresValidation(this SignInResult result) => result is ExtendedSignInResult { RequiresValidation: true };
}

internal sealed class TwoFactorAuthenticationInfo
{
    public string UserId { get; set; } = null!;
    public MfaDeviceIdentifier DeviceId { get; set; } = MfaDeviceIdentifier.Empty;
    public string? LoginProvider { get; set; }
}
