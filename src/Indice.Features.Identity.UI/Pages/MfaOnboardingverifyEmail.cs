using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.UI.Filters;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the MFA onboarding verify email screen.</summary>
[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.ExtendedValidationScheme)]
[UserActivityRequirementFilter<User>(UserActivityRequirementKind.RequiresMfaOnboarding)]
[IdentityUI(typeof(MfaOnboardingverifyEmailModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseMfaOnboardingVerifyEmailModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseMfaOnboardingVerifyEmailModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseMfaOnboardingVerifyEmailModel(
        ExtendedUserManager<User> userManager
    ) {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }

    /// <summary></summary>
    [BindProperty]
    public VerifyEmailInputModel Input { get; set; } = new VerifyEmailInputModel();

    /// <summary>Key used for setting and retrieving temp data.</summary>
    public static string TempDataKey => "mfa_onboarding_verify_email_alert";

    /// <summary>MFA onboarding verify email page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnGetAsync([FromQuery] string? returnUrl) {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        TempData.Put(TempDataKey, new ExtendedValidationTempDataModel {
            Alert = AlertModel.Success( UserManager.MessageDescriber.MfaVerifyEmailValidationMissingEmail),
            NextStepUrl = string.Empty
        });
        Input.Email = user.Email;
        Input.ReturnUrl = returnUrl;
        return Page();
    }

    /// <summary>MFA onboarding verify email page POST handler.</summary>
    public virtual async Task<IActionResult> OnPostAsync([FromQuery] string? returnUrl) {
        if (!ModelState.IsValid) {
            return Page();
        }
        var tempDataModel = new ExtendedValidationTempDataModel();
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        Input.Email = user.Email;
        //
        var result = await UserManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, Input.Code!.Trim());
        if (result) {
            user.EmailConfirmed = true;
            await UserManager.SetTwoFactorAsync(user, AuthenticationMethodType.Email.ToString());
            tempDataModel.Alert = AlertModel.Success(UserManager.MessageDescriber.MfaVerifyEmailSuccessMessage);
        } else {
            tempDataModel.Alert = AlertModel.Error(UserManager.MessageDescriber.MfaVerifyEmailValidationMissingEmail);
        }
        TempData.Put(TempDataKey, tempDataModel);
        return Page();
    }
}

internal class MfaOnboardingverifyEmailModel : BaseMfaOnboardingVerifyEmailModel
{
    public MfaOnboardingverifyEmailModel(
        ExtendedUserManager<User> userManager
    ) : base(userManager) { }
}
