using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.UI.Filters;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the MFA onboarding verify phone screen.</summary>
[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.ExtendedValidationScheme)]
[UserActivityRequirementFilter<User>(UserActivityRequirementKind.RequiresMfaOnboarding)]
[IdentityUI(typeof(MfaOnboardingVerifyPhoneModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseMfaOnboardingVerifyPhoneModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseMfaOnboardingVerifyPhoneModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseMfaOnboardingVerifyPhoneModel(
        ExtendedUserManager<User> userManager
    ) {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }

    /// <summary></summary>
    [BindProperty]
    public VerifyPhoneInputModel Input { get; set; } = new VerifyPhoneInputModel();

    /// <summary>Key used for setting and retrieving temp data.</summary>
    public static string TempDataKey => "mfa_onboarding_verify_phone_alert";

    /// <summary>MFA onboarding verify phone page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnGetAsync([FromQuery] string? returnUrl) {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        TempData.Put(TempDataKey, new ExtendedValidationTempDataModel {
            Alert = AlertModel.Success( UserManager.MessageDescriber.MfaVerifyPhoneValidationMissingPhone),
            NextStepUrl = string.Empty
        });
        Input.PhoneNumber = user.PhoneNumber;
        Input.ReturnUrl = returnUrl;
        return Page();
    }

    /// <summary>MFA onboarding verify phone page POST handler.</summary>
    public virtual async Task<IActionResult> OnPostAsync([FromQuery] string? returnUrl) {
        if (!ModelState.IsValid) {
            return Page();
        }
        var tempDataModel = new ExtendedValidationTempDataModel();
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        Input.PhoneNumber = user.PhoneNumber;
        var result = await UserManager.ChangePhoneNumberAsync(user, user.PhoneNumber!, Input.Code!);
        if (result.Succeeded) {
            await UserManager.SetTwoFactorAsync(user, AuthenticationMethodType.PhoneNumber.ToString());
            tempDataModel.Alert = AlertModel.Success(UserManager.MessageDescriber.MfaVerifyPhoneSuccessMessage);
        } else {
            tempDataModel.Alert = AlertModel.Error(UserManager.MessageDescriber.MfaVerifyPhoneValidationMissingPhone);
        }
        
        TempData.Put(TempDataKey, tempDataModel);
        return Page();
    }
}

internal class MfaOnboardingVerifyPhoneModel : BaseMfaOnboardingVerifyPhoneModel
{
    public MfaOnboardingVerifyPhoneModel(
        ExtendedUserManager<User> userManager
    ) : base(userManager) { }
}
