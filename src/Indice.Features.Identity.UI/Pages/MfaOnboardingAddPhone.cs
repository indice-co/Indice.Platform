using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Indice.Features.Identity.UI.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the MFA onboarding add phone screen.</summary>
[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.MfaOnboardingScheme)]
[IdentityUI(typeof(MfaOnboardingAddPhoneModel))]
[SecurityHeaders]
public abstract class BaseMfaOnboardingAddPhoneModel : BasePageModel
{
    private readonly IStringLocalizer<BaseMfaOnboardingAddPhoneModel> _localizer;

    /// <summary>Creates a new instance of <see cref="BaseMfaOnboardingAddPhoneModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseMfaOnboardingAddPhoneModel(
        ExtendedUserManager<User> userManager,
        IStringLocalizer<BaseMfaOnboardingAddPhoneModel> localizer
    ) {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }

    /// <summary>MFA onboarding add phone view model.</summary>
    public EnableMfaSmsViewModel View { get; set; } = new EnableMfaSmsViewModel();

    /// <summary>The input model that backs the MFA onboarding add phone page.</summary>
    [BindProperty]
    public EnableMfaSmsInputModel Input { get; set; } = new EnableMfaSmsInputModel();

    /// <summary>Key used for setting and retrieving temp data.</summary>
    public static string TempDataKey => "mfa_onboarding_add_phone_alert";

    /// <summary>MFA onboarding add phone page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnGetAsync([FromQuery] string? returnUrl) {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        var alert = user.PhoneNumberConfirmed && UserManager.StateProvider.CurrentState == UserState.RequiresMfaOnboarding
            ? _localizer["Your phone number is already confirmed. Continue to enable MFA."]
            : _localizer["Please select your phone number so we can verify it before we continue."];
        TempData.Put(TempDataKey, AlertModel.Info(alert));
        string? phoneCallingCode = null;
        var phoneNumber = user.PhoneNumber;
        if (GetPhoneCallingCodes() is not null && PhoneInfo.TryParse(phoneNumber, "GR", out var parsedPhoneNumber)) {
            phoneCallingCode = parsedPhoneNumber.CountryCode.ToString();
            phoneNumber = parsedPhoneNumber.NationalNumber.ToString();
        }
        Input = View = new EnableMfaSmsViewModel {
            PhoneCallingCode = phoneCallingCode,
            PhoneNumber = phoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            ReturnUrl = returnUrl
        };
        return Page();
    }

    /// <summary>MFA onboarding add phone page POST handler.</summary>
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> OnPostAsync([FromQuery] string? returnUrl) {
        if (!ModelState.IsValid) {
            return Page();
        }
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        IdentityResult result;
        if (!user.PhoneNumberConfirmed) {
            var phoneNumber = GetPhoneCallingCodes() is not null
                ? PhoneInfo.Format($"+{Input.PhoneCallingCode}{Input.PhoneNumber}")
                : Input.PhoneNumber;
            result = await UserManager.SetPhoneNumberAsync(user, phoneNumber);
            if (!result.Succeeded) {
                AddModelErrors(result);
                return Page();
            }
            await SendVerificationSmsAsync(user, phoneNumber!);
            return RedirectToPage("/MfaOnboardingVerifyPhone", routeValues: new { Input.ReturnUrl });
        }
        result = await UserManager.SetTwoFactorEnabledAsync(user, true);
        if (!result.Succeeded) {
            AddModelErrors(result);
            return Page();
        }
        TempData.Put(TempDataKey, AlertModel.Success(_localizer["You have successfully enabled MFA for your account. Login to access your account."]));
        View.NextStepUrl = GetRedirectUrl(UserManager.StateProvider.CurrentState, Input.ReturnUrl);
        View.PhoneNumberConfirmed = user.PhoneNumberConfirmed;
        return Page();
    }
}

internal class MfaOnboardingAddPhoneModel : BaseMfaOnboardingAddPhoneModel
{
    public MfaOnboardingAddPhoneModel(
        ExtendedUserManager<User> userManager,
        IStringLocalizer<MfaOnboardingAddPhoneModel> localizer
    ) : base(userManager, localizer) { }
}
