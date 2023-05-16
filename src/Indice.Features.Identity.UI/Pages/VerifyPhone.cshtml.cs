using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the extended validation add email screen.</summary>
[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.ExtendedValidationUserIdScheme)]
[IdentityUI(typeof(VerifyPhoneModel))]
[SecurityHeaders]
public abstract class BaseVerifyPhoneModel : BasePageModel
{
    private readonly IStringLocalizer<BaseVerifyPhoneModel> _localizer;
    private readonly ExtendedUserManager<User> _userManager;

    /// <summary>Creates a new instance of <see cref="BaseAddEmailModel"/> class.</summary>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="BaseAddEmailModel"/>.</param>
    /// <param name="userManager"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseVerifyPhoneModel(
        IStringLocalizer<BaseVerifyPhoneModel> localizer,
        ExtendedUserManager<User> userManager
    ) : base() {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <summary>The input model that backs the verify phone page.</summary>
    [BindProperty]
    public VerifyPhoneInputModel Input { get; set; } = new VerifyPhoneInputModel();

    /// <summary>Key used for setting and retrieving temp data.</summary>
    public static string TempDataKey => "info_message";

    /// <summary>Extended validation verify phone page GET handler.</summary>
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

    /// <summary>Extended validation verify phone page POST handler.</summary>
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> OnPostAsync() {
        if (!ModelState.IsValid) {
            return Page();
        }
        var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        var result = await _userManager.ChangePhoneNumberAsync(user, Input.PhoneNumber, Input.Code);
        if (result.Succeeded) {
            if (_userManager.StateProvider.CurrentState == UserState.LoggedIn) {
                await AutoSignIn(user, ExtendedIdentityConstants.ExtendedValidationUserIdScheme);
            }
            var redirectUrl = GetRedirectUrl(_userManager.StateProvider.CurrentState, Input.ReturnUrl) ?? "/";
            TempData.Put(TempDataKey, new ExtendedValidationTempDataModel {
                Alert = AlertModel.Success(_localizer["Your phone number was successfully validated. Please press the 'Next' button to continue."]),
                NextStepUrl = redirectUrl
            });
        } else {
            TempData.Put(TempDataKey, new ExtendedValidationTempDataModel {
                Alert = AlertModel.Error(_localizer["Please enter the code that you have received at your mobile phone."]),
                NextStepUrl = string.Empty
            });
        }
        return Page();
    }
}

internal class VerifyPhoneModel : BaseVerifyPhoneModel
{
    public VerifyPhoneModel(
        IStringLocalizer<VerifyPhoneModel> localizer,
        ExtendedUserManager<User> userManager
    ) : base(localizer, userManager) { }
}