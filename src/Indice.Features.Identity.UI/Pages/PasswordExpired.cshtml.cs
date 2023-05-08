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
[IdentityUI(typeof(PasswordExpiredModel))]
[SecurityHeaders]
public abstract class BasePasswordExpiredModel : BasePageModel
{
    private readonly IStringLocalizer<BasePasswordExpiredModel> _localizer;
    private readonly ExtendedUserManager<User> _userManager;

    /// <summary>Creates a new instance of <see cref="BasePasswordExpiredModel"/> class.</summary>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="BasePasswordExpiredModel"/>.</param>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BasePasswordExpiredModel(
        IStringLocalizer<BasePasswordExpiredModel> localizer,
        ExtendedUserManager<User> userManager
    ) : base() {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <summary>The input model that backs the password expired page.</summary>
    [BindProperty]
    public PasswordExpiredInputModel Input { get; set; } = new PasswordExpiredInputModel();

    /// <summary>Key used for setting and retrieving temp data.</summary>
    public static string TempDataKey => "info_message";

    /// <summary>Extended validation password expired page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnGetAsync([FromQuery] string? returnUrl) {
        await Task.CompletedTask;
        if (_userManager.StateProvider.CurrentState != UserState.RequiresPasswordChange) {
            return Redirect(GetRedirectUrl(_userManager.StateProvider.CurrentState, returnUrl) ?? "/");
        }
        TempData.Put(TempDataKey, new ExtendedValidationTempDataModel {
            Alert = AlertModel.Info(_localizer["Your password has expired. Please choose a new password."])
        });
        Input.ReturnUrl = returnUrl;
        return Page();
    }

    /// <summary>Extended validation password expired page POST handler.</summary>
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> OnPostAsync() {
        if (!ModelState.IsValid) {
            return Page();
        }
        var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        var result = await _userManager.ResetPasswordAsync(user, Input.NewPassword);
        if (!result.Succeeded) {
            AddModelErrors(result);
            return Page();
        }
        await _userManager.SetPasswordExpiredAsync(user, false);
        var infoMessage = _localizer["Your password has been changed successfully. Please press the 'Next' button to continue."];
        if (_userManager.StateProvider.CurrentState == UserState.LoggedIn) {
            await AutoSignIn(user, ExtendedIdentityConstants.ExtendedValidationUserIdScheme);
        }
        var redirectUrl = GetRedirectUrl(_userManager.StateProvider.CurrentState, Input.ReturnUrl);
        TempData.Put(nameof(infoMessage), new ExtendedValidationTempDataModel {
            Alert = AlertModel.Success(infoMessage),
            DisableForm = true,
            NextStepUrl = redirectUrl
        });
        return Page();
    }
}

internal class PasswordExpiredModel : BasePasswordExpiredModel
{
    public PasswordExpiredModel(
        IStringLocalizer<PasswordExpiredModel> localizer, 
        ExtendedUserManager<User> userManager
    ) : base(localizer, userManager) { }
}
