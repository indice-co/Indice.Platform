using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Filters;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the MFA onboarding authenticator app setup screen.</summary>
[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.ExtendedValidationScheme)]
[UserActivityRequirementFilter<User>(UserActivityRequirementKind.RequiresMfaOnboarding)]
[IdentityUI(typeof(MfaOnboardingSetupAuthenticatorModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseMfaOnboardingSetupAuthenticatorModel : BasePageModel
{
    private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

    /// <summary>Creates a new instance of <see cref="BaseMfaOnboardingSetupAuthenticatorModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseMfaOnboardingSetupAuthenticatorModel(
        ExtendedUserManager<User> userManager,
        IConfiguration configuration
    ) {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>Represents a set of key/value application configuration properties.</summary>
    protected IConfiguration Configuration { get; }

    /// <summary>MFA onboarding authenticator setup view model.</summary>
    public SetupAuthenticatorViewModel View { get; set; } = new SetupAuthenticatorViewModel();

    /// <summary>The input model that backs the MFA onboarding authenticator setup page.</summary>
    [BindProperty]
    public SetupAuthenticatorInputModel Input { get; set; } = new SetupAuthenticatorInputModel();

    /// <summary>Key used for storing recovery codes in temp data between the setup and recovery-codes pages.</summary>
    public static string RecoveryCodesTempDataKey => "mfa_onboarding_recovery_codes";

    /// <summary>MFA onboarding authenticator setup page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnGetAsync([FromQuery] string? returnUrl) {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        await LoadSharedKeyAndAuthenticatorUriAsync(user, returnUrl);
        return Page();
    }

    /// <summary>MFA onboarding authenticator setup page POST handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnPostAsync([FromQuery] string? returnUrl) {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        if (!ModelState.IsValid) {
            await LoadSharedKeyAndAuthenticatorUriAsync(user, returnUrl);
            return Page();
        }
        var verificationCode = (Input.Code ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
        var isTokenValid = await UserManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, verificationCode);
        if (!isTokenValid) {
            ModelState.AddModelError(nameof(Input.Code), UserManager.MessageDescriber.MfaValidationError);
            await LoadSharedKeyAndAuthenticatorUriAsync(user, returnUrl);
            return Page();
        }
        var setTwoFactorResult = await UserManager.SetTwoFactorEnabledAsync(user, true);
        if (!setTwoFactorResult.Succeeded) {
            AddModelErrors(setTwoFactorResult);
            await LoadSharedKeyAndAuthenticatorUriAsync(user, returnUrl);
            return Page();
        }
        var recoveryCodes = await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10) ?? Enumerable.Empty<string>();
        TempData.Put(RecoveryCodesTempDataKey, new RecoveryCodesViewModel {
            RecoveryCodes = recoveryCodes.ToArray(),
            ReturnUrl = Input.ReturnUrl ?? returnUrl
        });
        return RedirectToPage("/MfaOnboardingRecoveryCodes", routeValues: new { returnUrl = Input.ReturnUrl ?? returnUrl });
    }

    private async Task LoadSharedKeyAndAuthenticatorUriAsync(User user, string? returnUrl) {
        var unformattedKey = await UserManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(unformattedKey)) {
            await UserManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await UserManager.GetAuthenticatorKeyAsync(user);
        }
        var applicationName = Configuration.GetApplicationName() ?? "IdentityServer";
        var userIdentifier = await UserManager.GetEmailAsync(user) ?? user.UserName ?? string.Empty;
        View = new SetupAuthenticatorViewModel {
            Code = null,
            ReturnUrl = returnUrl ?? Input.ReturnUrl,
            SharedKey = unformattedKey,
            FormattedSharedKey = FormatSharedKey(unformattedKey!),
            AuthenticatorUri = BuildAuthenticatorUri(applicationName, userIdentifier, unformattedKey!)
        };
        Input.ReturnUrl ??= returnUrl;
    }

    private static string FormatSharedKey(string unformattedKey) {
        var result = new StringBuilder();
        var currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length) {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }
        if (currentPosition < unformattedKey.Length) {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }
        return result.ToString().ToLowerInvariant();
    }

    private static string BuildAuthenticatorUri(string applicationName, string userIdentifier, string unformattedKey) {
        return string.Format(
            CultureInfo.InvariantCulture,
            AuthenticatorUriFormat,
            UrlEncoder.Default.Encode(applicationName),
            UrlEncoder.Default.Encode(userIdentifier),
            unformattedKey
        );
    }
}

internal class MfaOnboardingSetupAuthenticatorModel : BaseMfaOnboardingSetupAuthenticatorModel
{
    public MfaOnboardingSetupAuthenticatorModel(
        ExtendedUserManager<User> userManager,
        IConfiguration configuration
    ) : base(userManager, configuration) { }
}
