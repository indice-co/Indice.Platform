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
[SecurityHeaders]
public class AddEmailPageModel : BasePageModel
{
    private readonly IStringLocalizer<AddEmailPageModel> _localizer;
    private readonly ExtendedUserManager<User> _userManager;

    /// <summary>Creates a new instance of <see cref="AddEmailPageModel"/> class.</summary>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="AddEmailPageModel"/>.</param>
    /// <param name="userManager"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public AddEmailPageModel(
        IStringLocalizer<AddEmailPageModel> localizer,
        ExtendedUserManager<User> userManager
    ) : base() {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <summary></summary>
    [TempData]
    public ExtendedValidationTempDataModel UiHint { get; set; } = new ExtendedValidationTempDataModel();

    /// <summary></summary>
    [BindProperty]
    public AddEmailInputModel Input { get; set; } = new AddEmailInputModel();

    /// <summary>Extended validation AddEmail page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public async Task<IActionResult> OnGetAsync([FromQuery] string? returnUrl) {
        var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        if (_userManager.StateProvider.CurrentState == UserState.LoggedIn) {
            await AutoSignIn(user, ExtendedIdentityConstants.ExtendedValidationUserIdScheme);
        }
        if (_userManager.StateProvider.CurrentState != UserState.RequiresEmailVerification) {
            return Redirect(GetRedirectUrl(_userManager.StateProvider.CurrentState, returnUrl) ?? "/");
        }
        UiHint.Alert = AlertModel.Info(_localizer["Please enter your email address so we can verify it before we continue."]);
        Input.Email = user.Email;
        Input.ReturnUrl = returnUrl;
        return Page();
    }

    /// <summary>Extended validation AddEmail page POST handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync([FromQuery] string? returnUrl) {
        if (!ModelState.IsValid) {
            return Page();
        }
        if (string.IsNullOrEmpty(returnUrl)) {
            returnUrl = Input.ReturnUrl;
        } else {
            Input.ReturnUrl = returnUrl;
        }
        var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        if (user.Email?.Equals(Input.Email) == false) {
            user.Email = Input.Email;
            var result = await _userManager.SetEmailAsync(user, Input.Email);
            if (!result.Succeeded) {
                AddModelErrors(result);
                return Page();
            }
        }
        await SendConfirmationEmail(user, returnUrl);
        UiHint.Alert = AlertModel.Success(_localizer["A confirmation email has been sent to the address bellow."]);
        UiHint.DisableForm = true;
        UiHint.NextStepUrl = Url.PageLink("AddEmail", values: new { returnUrl });
        return Page();
    }
}
