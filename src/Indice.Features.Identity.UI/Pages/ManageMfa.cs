using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>
/// Page model for the two-factor authentication management screen. Users can turn two-factor authentication on, change the method they use
/// (SMS/Viber, email or authenticator app) and generate new recovery codes. Turning two-factor authentication off is not supported.
/// When the user has MFA enabled, every change requires an MFA challenge.
/// </summary>
[Authorize]
[IdentityUI(typeof(ManageMfaModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseManageMfaModel : BaseProfileMfaChallengePageModel
{
    /// <summary>The temp data key used to pass the new recovery codes to the page that displays them.</summary>
    public const string RecoveryCodesTempDataKey = "manage_mfa_recovery_codes";
    /// <summary>The number of recovery codes that are generated.</summary>
    protected const int RecoveryCodesCount = 10;
    /// <summary>Authenticator apps always produce 6 digit codes.</summary>
    protected const int AuthenticatorDigits = 6;
    private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits={3}";
    private const string PendingKeyProtectorPurpose = "Indice.Features.Identity.UI.ManageMfa.PendingAuthenticatorKey";
    private static readonly TimeSpan PendingKeyLifetime = TimeSpan.FromMinutes(10);

    /// <summary>Creates a new instance of <see cref="BaseManageMfaModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="signInManager">Provides the APIs for user sign in.</param>
    /// <param name="authenticationMethodProvider">Abstracts interaction with system's various authentication methods.</param>
    /// <param name="dataProtectionProvider">Protects the pending authenticator key while the user sets up the authenticator app.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="identityUiOptions">Configuration options for Identity UI.</param>
    /// <param name="localizer">Provides localized labels for the Identity UI.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseManageMfaModel(
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        IAuthenticationMethodProvider authenticationMethodProvider,
        IDataProtectionProvider dataProtectionProvider,
        IConfiguration configuration,
        IOptions<IdentityUIOptions> identityUiOptions,
        IdentityUILocalizer localizer
    ) : base(userManager) {
        SignInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        AuthenticationMethodProvider = authenticationMethodProvider ?? throw new ArgumentNullException(nameof(authenticationMethodProvider));
        PendingKeyProtector = (dataProtectionProvider ?? throw new ArgumentNullException(nameof(dataProtectionProvider))).CreateProtector(PendingKeyProtectorPurpose).ToTimeLimitedDataProtector();
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        IdentityUiOptions = identityUiOptions?.Value ?? throw new ArgumentNullException(nameof(identityUiOptions));
        Localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>Provides the APIs for user sign in.</summary>
    protected ExtendedSignInManager<User> SignInManager { get; }
    /// <summary>Abstracts interaction with system's various authentication methods.</summary>
    protected IAuthenticationMethodProvider AuthenticationMethodProvider { get; }
    /// <summary>Protects the pending authenticator key while the user sets up the authenticator app.</summary>
    protected ITimeLimitedDataProtector PendingKeyProtector { get; }
    /// <summary>Represents a set of key/value application configuration properties.</summary>
    protected IConfiguration Configuration { get; }
    /// <summary>Configuration options for Identity UI.</summary>
    public IdentityUIOptions IdentityUiOptions { get; }
    /// <summary>Provides localized labels for the Identity UI.</summary>
    protected IdentityUILocalizer Localizer { get; }

    /// <summary>The two-factor authentication status and methods of the user.</summary>
    public ManageMfaViewModel View { get; set; } = new ManageMfaViewModel();
    /// <summary>When set, the authenticator app setup step is displayed.</summary>
    public ManageMfaSetupAuthenticatorViewModel? Setup { get; set; }
    /// <summary>When set, the new recovery codes are displayed.</summary>
    public RecoveryCodesViewModel? RecoveryCodes { get; set; }

    /// <inheritdoc />
    protected override bool IsEditDisabled => IdentityUiOptions.DisableMfaEdit;

    /// <inheritdoc />
    protected override async Task PreparePageAsync(User user) {
        var methods = await ManageMfaMethods.GetManageableMethodsAsync(AuthenticationMethodProvider);
        var twoFactorEnabled = await UserManager.GetTwoFactorEnabledAsync(user);
        var current = twoFactorEnabled ? await ManageMfaMethods.FindCurrentMethodAsync(AuthenticationMethodProvider, UserManager, user, methods) : null;
        var phoneNumber = await UserManager.GetPhoneNumberAsync(user);
        var phoneConfirmed = !string.IsNullOrWhiteSpace(phoneNumber) && await UserManager.IsPhoneNumberConfirmedAsync(user);
        var email = await UserManager.GetEmailAsync(user);
        var emailConfirmed = !string.IsNullOrWhiteSpace(email) && await UserManager.IsEmailConfirmedAsync(user);
        View = new ManageMfaViewModel {
            TwoFactorEnabled = twoFactorEnabled,
            CanRegenerateRecoveryCodes = await CanRegenerateRecoveryCodesAsync(user, twoFactorEnabled),
            Methods = methods.Select(method => new ManageMfaMethodViewModel {
                Type = method.Type,
                Code = method.Code,
                DisplayName = method.DisplayName,
                Detail = method.Type switch {
                    AuthenticationMethodType.PhoneNumber => phoneConfirmed ? phoneNumber : null,
                    AuthenticationMethodType.Email => emailConfirmed ? email : null,
                    _ => null
                },
                IsCurrent = current?.Type == method.Type,
                IsAvailable = method.Type switch {
                    AuthenticationMethodType.PhoneNumber => phoneConfirmed,
                    AuthenticationMethodType.Email => emailConfirmed,
                    _ => true
                }
            }).ToList()
        };
    }

    /// <summary>Two-factor authentication management page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        if (IsEditDisabled) {
            return RedirectToPage("/Profile");
        }
        var user = await GetUserAsync();
        await PreparePageAsync(user);
        await EnsureMfaChallengeAsync(user);
        TempData.Remove(RecoveryCodesTempDataKey);
        return Page();
    }

    /// <summary>Turns two-factor authentication on, or changes the method, to a method that sends a code (SMS/Viber or email).</summary>
    /// <param name="method">The selected method type.</param>
    /// <remarks>Switching away from the authenticator app removes the authenticator key, which also makes the recovery codes unusable.</remarks>
    public virtual async Task<IActionResult> OnPostSelectMethodAsync([FromForm] AuthenticationMethodType? method) {
        if (IsEditDisabled) {
            return BadRequest();
        }
        var user = await GetUserAsync();
        await PreparePageAsync(user);
        if (!await EnsureMfaChallengeAsync(user)) {
            ModelState.Clear();
            return Page();
        }
        var selected = View.Methods.FirstOrDefault(item => item.Type == method && item.Type != AuthenticationMethodType.AuthenticatorApp);
        if (selected is null || !selected.IsAvailable) {
            ModelState.AddModelError(string.Empty, Localizer.ManageMfa_MethodUnavailable.Value!);
            return Page();
        }
        if (selected.IsCurrent) {
            return RedirectToPage("/Profile");
        }
        var result = await UserManager.SetTwoFactorAsync(user, selected.Type.ToString());
        if (result.Succeeded && !string.IsNullOrWhiteSpace(await UserManager.GetAuthenticatorKeyAsync(user))) {
            // Otherwise the authenticator app, being more secure, would still be used to sign in.
            result = await UserManager.RemoveAuthenticatorKeyAsync(user);
        }
        if (!result.Succeeded) {
            AddModelErrors(result);
            await PreparePageAsync(user);
            return Page();
        }
        await SignInManager.RefreshSignInAsync(user);
        TempData.Put("Alert", AlertModel.Success(Localizer.ManageMfa_Updated.Value!));
        return RedirectToPage("/Profile");
    }

    /// <summary>Starts the authenticator app setup. A new key is created, but it is stored only after the user enters a valid code.</summary>
    public virtual async Task<IActionResult> OnPostSetupAuthenticatorAsync() {
        if (IsEditDisabled) {
            return BadRequest();
        }
        var user = await GetUserAsync();
        await PreparePageAsync(user);
        if (!await EnsureMfaChallengeAsync(user)) {
            ModelState.Clear();
            return Page();
        }
        if (!View.Methods.Any(item => item.Type == AuthenticationMethodType.AuthenticatorApp)) {
            ModelState.AddModelError(string.Empty, Localizer.ManageMfa_MethodUnavailable.Value!);
            return Page();
        }
        var key = UserManager.GenerateNewAuthenticatorKey();
        var protectedKey = PendingKeyProtector.Protect($"{user.Id}|{key}", PendingKeyLifetime);
        Setup = await BuildSetupModelAsync(user, key, protectedKey);
        return Page();
    }

    /// <summary>Completes the authenticator app setup. The new key is stored when the code is valid, and new recovery codes are generated.</summary>
    /// <param name="protectedKey">The protected pending authenticator key.</param>
    /// <param name="code">The code produced by the authenticator app.</param>
    public virtual async Task<IActionResult> OnPostVerifyAuthenticatorAsync([FromForm] string? protectedKey, [FromForm] string? code) {
        if (IsEditDisabled) {
            return BadRequest();
        }
        var user = await GetUserAsync();
        await PreparePageAsync(user);
        if (!await EnsureMfaChallengeAsync(user)) {
            ModelState.Clear();
            return Page();
        }
        ModelState.Clear();
        var key = UnprotectPendingKey(user, protectedKey, out var expired);
        if (key is null) {
            if (expired) {
                ModelState.AddModelError(string.Empty, Localizer.ManageMfa_SetupExpired.Value!);
                return Page();
            }
            return BadRequest();
        }
        var result = await UserManager.ReplaceAuthenticatorKeyAsync(user, key, code);
        if (!result.Succeeded) {
            ModelState.AddModelError(nameof(code), UserManager.MessageDescriber.MfaValidationError);
            Setup = await BuildSetupModelAsync(user, key, protectedKey!);
            return Page();
        }
        result = await UserManager.SetTwoFactorAsync(user, AuthenticationMethodType.AuthenticatorApp.ToString());
        if (!result.Succeeded) {
            AddModelErrors(result);
            await PreparePageAsync(user);
            return Page();
        }
        await SignInManager.RefreshSignInAsync(user);
        if (!await IsRecoveryCodeMethodEnabledAsync()) {
            TempData.Put("Alert", AlertModel.Success(Localizer.ManageMfa_Updated.Value!));
            return RedirectToPage("/Profile");
        }
        return await ShowNewRecoveryCodesAsync(user);
    }

    /// <summary>Generates new recovery codes. The previous codes stop working.</summary>
    public virtual async Task<IActionResult> OnPostRegenerateRecoveryCodesAsync() {
        if (IsEditDisabled) {
            return BadRequest();
        }
        var user = await GetUserAsync();
        await PreparePageAsync(user);
        if (!await EnsureMfaChallengeAsync(user)) {
            ModelState.Clear();
            return Page();
        }
        if (!View.CanRegenerateRecoveryCodes) {
            return BadRequest();
        }
        return await ShowNewRecoveryCodesAsync(user);
    }

    /// <summary>Displays the new recovery codes.</summary>
    public virtual async Task<IActionResult> OnGetRecoveryCodesAsync() {
        if (IsEditDisabled) {
            return RedirectToPage("/Profile");
        }
        var user = await GetUserAsync();
        var codes = await PeekRecoveryCodesAsync(user);
        if (codes is null) {
            return RedirectToPage();
        }
        await PreparePageAsync(user);
        RecoveryCodes = codes;
        return Page();
    }

    /// <summary>Downloads the new recovery codes as a text file.</summary>
    public virtual async Task<IActionResult> OnGetDownloadRecoveryCodesAsync() {
        if (IsEditDisabled) {
            return NotFound();
        }
        var user = await GetUserAsync();
        var codes = await PeekRecoveryCodesAsync(user);
        if (codes is null) {
            return NotFound();
        }
        var text = codes.ToString(Localizer.ApplicationName(Configuration.GetApplicationName() ?? string.Empty), Localizer.MfaOnBoardingRecoveryCodes_FileHeader(codes.UserName ?? string.Empty).Value);
        return File(Encoding.UTF8.GetBytes(text), "text/plain", "recovery-codes.txt");
    }

    /// <summary>Removes the new recovery codes from temp data, once the user has saved them.</summary>
    public virtual IActionResult OnPostRecoveryCodesDone() {
        TempData.Remove(RecoveryCodesTempDataKey);
        TempData.Put("Alert", AlertModel.Success(Localizer.ManageMfa_Updated.Value!));
        return RedirectToPage("/Profile");
    }

    /// <summary>Formats the authenticator key in groups of four lowercase characters.</summary>
    /// <param name="key">The Base32 authenticator key.</param>
    protected static string FormatKey(string key) {
        var result = new StringBuilder();
        for (var position = 0; position < key.Length; position += 4) {
            if (position > 0) {
                result.Append(' ');
            }
            result.Append(key.AsSpan(position, Math.Min(4, key.Length - position)));
        }
        return result.ToString().ToLowerInvariant();
    }

    private async Task<ManageMfaSetupAuthenticatorViewModel> BuildSetupModelAsync(User user, string key, string protectedKey) {
        var applicationName = Configuration.GetApplicationName() ?? "IdentityServer";
        var userIdentifier = await UserManager.GetEmailAsync(user) ?? await UserManager.GetUserNameAsync(user) ?? string.Empty;
        return new ManageMfaSetupAuthenticatorViewModel {
            ProtectedKey = protectedKey,
            SharedKey = key,
            FormattedSharedKey = FormatKey(key),
            AuthenticatorDigits = AuthenticatorDigits,
            AuthenticatorUri = string.Format(
                CultureInfo.InvariantCulture,
                AuthenticatorUriFormat,
                UrlEncoder.Default.Encode(applicationName),
                UrlEncoder.Default.Encode(userIdentifier),
                key,
                AuthenticatorDigits
            )
        };
    }

    private string? UnprotectPendingKey(User user, string? protectedKey, out bool expired) {
        expired = false;
        if (string.IsNullOrWhiteSpace(protectedKey)) {
            return null;
        }
        string payload;
        try {
            payload = PendingKeyProtector.Unprotect(protectedKey);
        } catch (CryptographicException) {
            // Expired or tampered with. Either way the user has to start again.
            expired = true;
            return null;
        }
        var separator = payload.IndexOf('|');
        if (separator <= 0 || !string.Equals(payload[..separator], user.Id, StringComparison.Ordinal)) {
            return null;
        }
        return payload[(separator + 1)..];
    }

    private async Task<bool> IsRecoveryCodeMethodEnabledAsync() =>
        (await AuthenticationMethodProvider.GetMfaMethodsAsync()).Any(method => method.Type == AuthenticationMethodType.RecoveryCode);

    private async Task<bool> CanRegenerateRecoveryCodesAsync(User user, bool twoFactorEnabled) =>
        twoFactorEnabled &&
        !string.IsNullOrWhiteSpace(await UserManager.GetAuthenticatorKeyAsync(user)) &&
        await IsRecoveryCodeMethodEnabledAsync();

    private async Task<IActionResult> ShowNewRecoveryCodesAsync(User user) {
        var codes = await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodesCount) ?? [];
        TempData.Put(RecoveryCodesTempDataKey, new RecoveryCodesViewModel {
            RecoveryCodes = codes.ToArray(),
            UserName = await UserManager.GetUserNameAsync(user)
        });
        return RedirectToPage(pageName: null, pageHandler: "RecoveryCodes");
    }

    private async Task<RecoveryCodesViewModel?> PeekRecoveryCodesAsync(User user) {
        var codes = TempData.Peek<RecoveryCodesViewModel>(RecoveryCodesTempDataKey);
        if (codes?.RecoveryCodes is not { Length: > 0 } || !string.Equals(codes.UserName, await UserManager.GetUserNameAsync(user), StringComparison.Ordinal)) {
            return null;
        }
        return codes;
    }
}

internal class ManageMfaModel : BaseManageMfaModel
{
    public ManageMfaModel(
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        IAuthenticationMethodProvider authenticationMethodProvider,
        IDataProtectionProvider dataProtectionProvider,
        IConfiguration configuration,
        IOptions<IdentityUIOptions> identityUiOptions,
        IdentityUILocalizer localizer
    ) : base(userManager, signInManager, authenticationMethodProvider, dataProtectionProvider, configuration, identityUiOptions, localizer) { }
}

/// <summary>Helpers for the two-factor authentication methods users can manage on their own.</summary>
internal static class ManageMfaMethods
{
    private static readonly AuthenticationMethodType[] ManageableTypes = [
        AuthenticationMethodType.PhoneNumber,
        AuthenticationMethodType.Email,
        AuthenticationMethodType.AuthenticatorApp
    ];

    /// <summary>Gets the enabled methods users can choose, one per type (for example SMS or Viber for the phone number).</summary>
    public static async Task<AuthenticationMethod[]> GetManageableMethodsAsync(IAuthenticationMethodProvider provider) =>
        (await provider.GetMfaMethodsAsync())
            .Where(method => method.SupportsMfaOnboarding && ManageableTypes.Contains(method.Type))
            .GroupBy(method => method.Type)
            .Select(group => group.First())
            .OrderBy(method => Array.IndexOf(ManageableTypes, method.Type))
            .ToArray();

    /// <summary>Finds which of the <paramref name="manageableMethods"/> is used when the user signs in.</summary>
    public static async Task<AuthenticationMethod?> FindCurrentMethodAsync(IAuthenticationMethodProvider provider, ExtendedUserManager<User> userManager, User user, IReadOnlyCollection<AuthenticationMethod> manageableMethods) {
        var types = manageableMethods.Select(method => method.Type).ToHashSet();
        var userMethods = (await provider.GetAllMethodsForUserAsync(user)).Where(method => types.Contains(method.Type));
        var current = userMethods.SelectPreferred(await userManager.GetTwoFactorPreferenceAsync(user));
        return current is null ? null : manageableMethods.First(method => method.Type == current.Type);
    }
}
