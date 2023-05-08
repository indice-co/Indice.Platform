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
[IdentityUI(typeof(AddEmailModel))]
[SecurityHeaders]
public abstract class BaseAddEmailModel : BasePageModel
{
    private readonly IStringLocalizer<BaseAddEmailModel> _localizer;
    private readonly ExtendedUserManager<User> _userManager;

    /// <summary>Creates a new instance of <see cref="BaseAddEmailModel"/> class.</summary>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="BaseAddEmailModel"/>.</param>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseAddEmailModel(
        IStringLocalizer<BaseAddEmailModel> localizer,
        ExtendedUserManager<User> userManager
    ) : base() {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <summary>The input model that backs the add email page.</summary>
    [BindProperty]
    public AddEmailInputModel Input { get; set; } = new AddEmailInputModel();

    /// <summary>Key used for setting and retrieving temp data.</summary>
    public static string TempDataKey => "info_message";

    /// <summary>Extended validation add email page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnGetAsync([FromQuery] string? returnUrl) {
        var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        if (_userManager.StateProvider.CurrentState == UserState.LoggedIn) {
            await AutoSignIn(user, ExtendedIdentityConstants.ExtendedValidationUserIdScheme);
        }
        if (_userManager.StateProvider.CurrentState != UserState.RequiresEmailVerification) {
            return Redirect(GetRedirectUrl(_userManager.StateProvider.CurrentState, returnUrl) ?? "/");
        }
        TempData.Put(TempDataKey, new ExtendedValidationTempDataModel {
            Alert = AlertModel.Info(_localizer["Please enter your email address so we can verify it before we continue."])
        });
        Input.Email = user.Email;
        Input.ReturnUrl = returnUrl;
        return Page();
    }

    /// <summary>Extended validation add email page POST handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> OnPostAsync([FromQuery] string? returnUrl) {
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
        TempData.Put(TempDataKey, new ExtendedValidationTempDataModel {
            Alert = AlertModel.Success(_localizer["A confirmation email has been sent to the address bellow."]),
            DisableForm = true,
            NextStepUrl = Url.PageLink("AddEmail", values: new { returnUrl })
        });
        return Page();
    }
}

internal class AddEmailModel : BaseAddEmailModel
{
    public AddEmailModel(
        IStringLocalizer<AddEmailModel> localizer, 
        ExtendedUserManager<User> userManager
    ) : base(localizer, userManager) { }
}