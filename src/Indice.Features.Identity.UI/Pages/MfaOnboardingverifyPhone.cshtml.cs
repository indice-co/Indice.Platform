using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the MFA onboarding verify phone screen.</summary>
[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.MfaOnboardingScheme)]
[IdentityUI(typeof(MfaOnboardingVerifyPhoneModel))]
[SecurityHeaders]
public abstract class BaseMfaOnboardingVerifyPhoneModel : BasePageModel
{
    private readonly ExtendedUserManager<User> _userManager;
    private readonly IStringLocalizer<BaseMfaOnboardingVerifyPhoneModel> _localizer;

    /// <summary>Creates a new instance of <see cref="BaseMfaOnboardingVerifyPhoneModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseMfaOnboardingVerifyPhoneModel(
        ExtendedUserManager<User> userManager,
        IStringLocalizer<BaseMfaOnboardingVerifyPhoneModel> localizer
    ) {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary></summary>
    [BindProperty]
    public VerifyPhoneInputModel Input { get; set; } = new VerifyPhoneInputModel();

    /// <summary>Key used for setting and retrieving temp data.</summary>
    public static string TempDataKey => "mfa_onboarding_verify_phone_alert";

    /// <summary>MFA onboarding verify phone page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnGetAsync([FromQuery] string? returnUrl) {
        var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        TempData.Put(TempDataKey, new ExtendedValidationTempDataModel {
            Alert = AlertModel.Success(_localizer["Please enter the code that you have received at your mobile phone."]),
            NextStepUrl = string.Empty
        });
        Input.PhoneNumber = user.PhoneNumber;
        Input.ReturnUrl = returnUrl;
        return Page();
    }

    /// <summary>MFA onboarding verify phone page POST handler.</summary>
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> OnPostAsync() {
        if (!ModelState.IsValid) {
            return Page();
        }
        var tempDataModel = new ExtendedValidationTempDataModel();
        var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        Input.PhoneNumber = user.PhoneNumber;
        var result = await _userManager.ChangePhoneNumberAsync(user, user.PhoneNumber, Input.Code);
        if (result.Succeeded) {
            await _userManager.SetTwoFactorEnabledAsync(user, true);
            tempDataModel.Alert = AlertModel.Success(_localizer["Your phone number was successfully validated. Please press the 'Next' button to continue."]);
        } else {
            tempDataModel.Alert = AlertModel.Error(_localizer["Please enter the code that you have received at your mobile phone."]);
        }
        tempDataModel.NextStepUrl = GetRedirectUrl(_userManager.StateProvider.CurrentState, Input.ReturnUrl) ?? "/";
        TempData.Put(TempDataKey, tempDataModel);
        return Page();
    }
}

internal class MfaOnboardingVerifyPhoneModel : BaseMfaOnboardingVerifyPhoneModel
{
    public MfaOnboardingVerifyPhoneModel(
        ExtendedUserManager<User> userManager,
        IStringLocalizer<MfaOnboardingVerifyPhoneModel> localizer
    ) : base(userManager, localizer) { }
}
