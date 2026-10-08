using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Filters;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the extended validation add email screen.</summary>
[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.ExtendedValidationScheme)]
[UserActivityRequirementFilter<User>(UserActivityRequirementKind.RequiresPhoneNumberVerification)]
[IdentityUI(typeof(VerifyPhoneModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseVerifyPhoneModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseAddEmailModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="signInManager"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseVerifyPhoneModel(
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager
    ) : base() {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        SignInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>Provides the APIs for user sign in.</summary>
    protected ExtendedSignInManager<User> SignInManager { get; }

    /// <summary>The input model that backs the verify phone page.</summary>
    [BindProperty]
    public VerifyPhoneInputModel Input { get; set; } = new VerifyPhoneInputModel();

    /// <summary>Key used for setting and retrieving temp data.</summary>
    public static string TempDataKey => "info_message";

    /// <summary>Extended validation verify phone page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnGetAsync([FromQuery] string? returnUrl) {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        TempData.Put(TempDataKey, new ExtendedValidationTempDataModel {
            Alert = AlertModel.Success(UserManager.MessageDescriber.RegisterPhoneConfirmationPrompt),
            NextStepUrl = string.Empty
        });
        Input.PhoneNumber = user.PhoneNumber;
        Input.ReturnUrl = returnUrl;
        return Page();
    }

    /// <summary>Extended validation verify phone page POST handler.</summary>
    public virtual async Task<IActionResult> OnPostAsync([FromQuery] string? returnUrl) {
        if (!ModelState.IsValid) {
            return Page();
        }
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        if (Input.OtpResend) {
            var sent = await SendVerificationSmsAsync(user, Input.PhoneNumber!);
            if (!sent) {
                TempData.Put(TempDataKey, new ExtendedValidationTempDataModel {
                    Alert = AlertModel.Error( UserManager.MessageDescriber.LimitAttemptsReached),
                    NextStepUrl = string.Empty
                });
            }
            return Page();
        }
        var result = await UserManager.ChangePhoneNumberAsync(user, Input.PhoneNumber!, Input.Code!);
        if (result.Succeeded) {
            // next step or signin
        } else {
            TempData.Put(TempDataKey, new ExtendedValidationTempDataModel {
                Alert = AlertModel.Error(UserManager.MessageDescriber.RegisterPhoneConfirmationPrompt),
                NextStepUrl = string.Empty
            });
        }
        return Page();
    }
}

internal class VerifyPhoneModel : BaseVerifyPhoneModel
{
    public VerifyPhoneModel(
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager
    ) : base(userManager, signInManager) { }
}
