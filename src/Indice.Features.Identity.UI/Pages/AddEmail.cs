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
[UserActivityRequirementFilter<User>(UserActivityRequirementKind.RequiresEmailVerification)]
[IdentityUI(typeof(AddEmailModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseAddEmailModel : BasePageModel
{

    /// <summary>Creates a new instance of <see cref="BaseAddEmailModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="signInManager"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseAddEmailModel(
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

    /// <summary>The input model that backs the add email page.</summary>
    [BindProperty]
    public AddEmailInputModel Input { get; set; } = new AddEmailInputModel();

    /// <summary>Key used for setting and retrieving temp data.</summary>
    public static string TempDataKey => "add_email_info_message";

    /// <summary>Extended validation add email page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnGetAsync([FromQuery] string? returnUrl) {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        Input.Email = user.Email;
        Input.ReturnUrl = returnUrl;
        if (!UiOptions.ShowAddEmailPrompt) {
            return await OnPostAsync(returnUrl);
        }
        TempData.Put(TempDataKey, new ExtendedValidationTempDataModel {
            Alert = AlertModel.Info(UserManager.MessageDescriber.AddEmailValidationEmailEmpty)
        });
        return Page();
    }

    /// <summary>Extended validation add email page POST handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnPostAsync([FromQuery] string? returnUrl) {
        if (!ModelState.IsValid) {
            return Page();
        }
        if (string.IsNullOrEmpty(returnUrl)) {
            returnUrl = Input.ReturnUrl;
        } else {
            Input.ReturnUrl = returnUrl;
        }
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        if (user.Email?.Equals(Input.Email) == false) {
            user.Email = Input.Email;
            var result = await UserManager.SetEmailAsync(user, Input.Email);
            if (!result.Succeeded) {
                AddModelErrors(result);
                return Page();
            }
        }
        await SendConfirmationEmail(user, returnUrl);
        TempData.Put(TempDataKey, new ExtendedValidationTempDataModel {
            Alert = AlertModel.Success(UserManager.MessageDescriber.AddEmailConfirmationEmailSend),
            DisableForm = true,
            NextStepUrl = Url.PageLink("/AddEmail", values: new { returnUrl })
        });
        return Page();
    }
}

[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.ExtendedValidationScheme)]
internal class AddEmailModel : BaseAddEmailModel
{
    public AddEmailModel(
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager
    ) : base(userManager, signInManager) { }
}