using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Filters;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the extended validation verify email screen, used when <see cref="IdentityUIOptions.EmailConfirmationMethod"/> is <see cref="EmailConfirmationMethod.Otp"/>.</summary>
[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.ExtendedValidationScheme)]
[UserActivityRequirementFilter<User>(UserActivityRequirementKind.RequiresEmailVerification)]
[IdentityUI(typeof(VerifyEmailModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseVerifyEmailModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseVerifyEmailModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseVerifyEmailModel(ExtendedUserManager<User> userManager) : base() {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }

    /// <summary>The input model that backs the verify email page.</summary>
    [BindProperty]
    public VerifyEmailInputModel Input { get; set; } = new VerifyEmailInputModel();

    /// <summary>Key used for setting and retrieving temp data.</summary>
    public static string TempDataKey => "verify_email_info_message";

    /// <summary>Extended validation verify email page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnGetAsync([FromQuery] string? returnUrl) {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        Input.Email = user.Email;
        Input.ReturnUrl = SanitizeReturnUrl(returnUrl);
        TempData.Remove(TempDataKey);
        return Page();
    }

    /// <summary>Extended validation verify email page POST handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnPostAsync([FromQuery] string? returnUrl) {
        if (string.IsNullOrEmpty(returnUrl)) {
            returnUrl = Input.ReturnUrl;
        }
        Input.ReturnUrl = returnUrl = SanitizeReturnUrl(returnUrl);
        TempData.Remove(TempDataKey);
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        Input.Email = user.Email;
        if (Input.OtpResend) {
            Input.OtpResend = false;
            ModelState.Clear();
            if (!await SendConfirmationOtpEmail(user)) {
                TempData.Put(TempDataKey, new ExtendedValidationTempDataModel {
                    Alert = AlertModel.Error(UserManager.MessageDescriber.LimitAttemptsReached),
                    NextStepUrl = string.Empty
                });
            }
            return Page();
        }
        if (string.IsNullOrWhiteSpace(Input.Code)) {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Code)}", IdentityLabels.VerifyEmail_InvalidCode);
            return Page();
        }
        var result = await UserManager.ConfirmEmailWithOtpAsync(user, Input.Code);
        if (!result.Succeeded) {
            TempData.Put(TempDataKey, new ExtendedValidationTempDataModel {
                Alert = AlertModel.Error(IdentityLabels.VerifyEmail_InvalidCode),
                NextStepUrl = string.Empty
            });
            return Page();
        }
        Input.Code = null;
        TempData.Put(TempDataKey, new ExtendedValidationTempDataModel {
            Alert = AlertModel.Success(IdentityLabels.VerifyEmail_Success),
            DisableForm = true,
            NextStepUrl = Url.PageLink("/AddEmail", values: new { returnUrl })
        });
        return Page();
    }
}

internal class VerifyEmailModel : BaseVerifyEmailModel
{
    public VerifyEmailModel(ExtendedUserManager<User> userManager) : base(userManager) { }
}
