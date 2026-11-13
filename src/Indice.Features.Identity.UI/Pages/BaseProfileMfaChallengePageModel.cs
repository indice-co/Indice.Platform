using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Configuration;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>
/// Base page model for profile pages that change sensitive data (email, phone number and username).
/// When the user has MFA enabled, changes are allowed only after a successful MFA challenge that is valid for <see cref="IdentityUIOptions.ProfileMfaChallengeLifetime"/>.
/// </summary>
public abstract class BaseProfileMfaChallengePageModel : BasePageModel
{
    /// <summary>The model state key used for MFA challenge errors.</summary>
    public const string MfaChallengeCodeKey = "MfaChallengeCode";

    private IProfileMfaChallengeService? _mfaChallengeService;

    /// <summary>Creates a new instance of <see cref="BaseProfileMfaChallengePageModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <exception cref="ArgumentNullException"></exception>
    protected BaseProfileMfaChallengePageModel(ExtendedUserManager<User> userManager) : base() {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>The profile MFA challenge service.</summary>
    protected IProfileMfaChallengeService MfaChallengeService => _mfaChallengeService ??= ServiceProvider.GetRequiredService<IProfileMfaChallengeService>();
    /// <summary>The MFA challenge dialog model. When set, the MFA challenge dialog is displayed and the edit form is disabled.</summary>
    public ProfileMfaChallengeViewModel? MfaChallenge { get; set; }

    /// <summary>Indicates that editing is disabled by configuration.</summary>
    protected abstract bool IsEditDisabled { get; }

    /// <summary>Populates the properties needed to render the page (for example the current value).</summary>
    /// <param name="user">The signed in user.</param>
    protected abstract Task PreparePageAsync(User user);

    /// <summary>Checks whether the user may change their data. When an MFA challenge is needed, <see cref="MfaChallenge"/> is populated so the dialog is displayed.</summary>
    /// <param name="user">The signed in user.</param>
    /// <returns>True if the change is allowed, otherwise false.</returns>
    protected virtual async Task<bool> EnsureMfaChallengeAsync(User user) {
        var status = await MfaChallengeService.GetStatusAsync(user);
        if (status is ProfileMfaChallengeStatus.NotRequired or ProfileMfaChallengeStatus.Satisfied) {
            return true;
        }
        MfaChallenge = await BuildMfaChallengeModelAsync(user, status, methodCode: null, codeSent: false);
        return false;
    }

    /// <summary>Switches the MFA challenge dialog to another method without sending a code. Only allowed when method selection is allowed.</summary>
    /// <param name="mfaChallengeMethod">The code of the selected method.</param>
    public virtual async Task<IActionResult> OnPostMfaChallengeSelectAsync([FromForm] string? mfaChallengeMethod) {
        if (IsEditDisabled) {
            return BadRequest();
        }
        var user = await GetUserAsync();
        ModelState.Clear();
        await PreparePageAsync(user);
        var status = await MfaChallengeService.GetStatusAsync(user);
        if (status is ProfileMfaChallengeStatus.NotRequired or ProfileMfaChallengeStatus.Satisfied) {
            return RedirectToPage();
        }
        MfaChallenge = await BuildMfaChallengeModelAsync(user, status, mfaChallengeMethod, codeSent: false);
        return Page();
    }

    /// <summary>Sends an MFA challenge code through the selected method. For methods without a delivery channel (authenticator app) it only selects the method.</summary>
    /// <param name="mfaChallengeMethod">The code of the selected method.</param>
    public virtual async Task<IActionResult> OnPostMfaChallengeSendAsync([FromForm] string? mfaChallengeMethod) {
        if (IsEditDisabled) {
            return BadRequest();
        }
        var user = await GetUserAsync();
        ModelState.Clear();
        await PreparePageAsync(user);
        var status = await MfaChallengeService.GetStatusAsync(user);
        if (status is ProfileMfaChallengeStatus.NotRequired or ProfileMfaChallengeStatus.Satisfied) {
            return RedirectToPage();
        }
        MfaChallenge = await BuildMfaChallengeModelAsync(user, status, mfaChallengeMethod, codeSent: false);
        if (MfaChallenge.IsBlocked) {
            return Page();
        }
        var result = await MfaChallengeService.SendCodeAsync(user, MfaChallenge.SelectedMethod!);
        if (result.Success) {
            MfaChallenge.CodeSent = MfaChallenge.RequiresSending;
        } else if (result.IsRateLimited) {
            // A code has already been sent and is still valid.
            MfaChallenge.CodeSent = true;
            ModelState.AddModelError(MfaChallengeCodeKey, UserManager.MessageDescriber.MfaTokenNotExpired);
        } else {
            ModelState.AddModelError(MfaChallengeCodeKey, result.Error ?? UserManager.MessageDescriber.MfaValidationError);
        }
        return Page();
    }

    /// <summary>Verifies the MFA challenge code. On success the page is reloaded and editing is allowed.</summary>
    /// <param name="mfaChallengeMethod">The code of the selected method.</param>
    /// <param name="mfaChallengeCode">The code entered by the user.</param>
    public virtual async Task<IActionResult> OnPostMfaChallengeVerifyAsync([FromForm] string? mfaChallengeMethod, [FromForm] string? mfaChallengeCode) {
        if (IsEditDisabled) {
            return BadRequest();
        }
        var user = await GetUserAsync();
        ModelState.Clear();
        await PreparePageAsync(user);
        var status = await MfaChallengeService.GetStatusAsync(user);
        if (status is ProfileMfaChallengeStatus.NotRequired or ProfileMfaChallengeStatus.Satisfied) {
            return RedirectToPage();
        }
        MfaChallenge = await BuildMfaChallengeModelAsync(user, status, mfaChallengeMethod, codeSent: true);
        if (MfaChallenge.IsBlocked) {
            return Page();
        }
        var result = await MfaChallengeService.VerifyCodeAsync(user, MfaChallenge.SelectedMethod!, mfaChallengeCode ?? string.Empty);
        switch (result) {
            case ProfileMfaChallengeVerifyResult.Succeeded:
                return RedirectToPage();
            case ProfileMfaChallengeVerifyResult.LockedOut:
                var signInManager = ServiceProvider.GetRequiredService<ExtendedSignInManager<User>>();
                await signInManager.SignOutAsync();
                return RedirectToPage("/Login");
            default:
                ModelState.AddModelError(MfaChallengeCodeKey, UserManager.MessageDescriber.MfaValidationError);
                return Page();
        }
    }

    /// <summary>Gets the signed in user.</summary>
    /// <exception cref="InvalidOperationException">The user cannot be found.</exception>
    protected async Task<User> GetUserAsync() => await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");

    private async Task<ProfileMfaChallengeViewModel> BuildMfaChallengeModelAsync(User user, ProfileMfaChallengeStatus status, string? methodCode, bool codeSent) {
        var methods = status == ProfileMfaChallengeStatus.Blocked ? [] : await MfaChallengeService.GetMethodsAsync(user);
        var selectedMethod = status == ProfileMfaChallengeStatus.Blocked ? null : await MfaChallengeService.FindMethodAsync(user, methodCode);
        // Delivered codes (SMS, Viber, email) use the configured TOTP length; authenticator apps always produce 6 digit codes.
        var codeLength = selectedMethod?.SupportsDeliveryChannel() == true
            ? ServiceProvider.GetService<IOptions<TotpOptions>>()?.Value.CodeLength ?? TotpOptionsBase.DefaultCodeLength
            : TotpOptionsBase.DefaultCodeLength;
        return new ProfileMfaChallengeViewModel {
            Status = status,
            Methods = methods,
            SelectedMethod = selectedMethod,
            AllowMethodSelection = MfaChallengeService.AllowMethodSelection,
            CodeSent = codeSent,
            CodeLength = codeLength
        };
    }
}
