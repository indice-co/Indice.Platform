using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Filters;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the extended validation add email screen.</summary>
[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.ExtendedValidationScheme)]
[UserActivityRequirementFilter<User>(UserActivityRequirementKind.RequiresPasswordChange)]
[IdentityUI(typeof(PasswordExpiredModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BasePasswordExpiredModel : BasePageModel
{

    /// <summary>Creates a new instance of <see cref="BasePasswordExpiredModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="signInManager"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public BasePasswordExpiredModel(
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

    /// <summary>The input model that backs the password expired page.</summary>
    [BindProperty]
    public PasswordExpiredInputModel Input { get; set; } = new PasswordExpiredInputModel();

    /// <summary>View model for the password-expired page.</summary>
    public PasswordExpiredViewModel View { get; set; } = new PasswordExpiredViewModel();

    /// <summary>Key used for setting and retrieving temp data.</summary>
    public static string TempDataKey => "info_message";

    /// <summary>Extended validation password expired page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnGetAsync([FromQuery] string? returnUrl) {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        var message = user.LastSignInDate is null ?
            UserManager.MessageDescriber.PasswordExpiredFirstTimeUserMessage :
            UserManager.MessageDescriber.PasswordExpiredMessage;
        TempData.Put(TempDataKey, new ExtendedValidationTempDataModel {
            Alert = AlertModel.Info(message)
        });
        Input.ReturnUrl = returnUrl;
        return Page();
    }

    /// <summary>Extended validation password expired page POST handler.</summary>
    public virtual async Task<IActionResult> OnPostAsync([FromQuery] string? returnUrl) {
        if (!ModelState.IsValid) {
            return Page();
        }
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        var result = await UserManager.ResetPasswordAsync(user, Input.NewPassword!, isAdminOperation: false);
        if (!result.Succeeded) {
            AddModelErrors(result);
            return Page();
        }
        await UserManager.SetPasswordExpiredAsync(user, false);
        TempData.Put(TempDataKey, new ExtendedValidationTempDataModel {
            Alert = AlertModel.Success(UserManager.MessageDescriber.PasswordChangedSuccessfully),
            DisableForm = true,
            NextStepUrl = Url.Page("/PasswordExpired", new { returnUrl })
        });
        return Page();
    }
}

internal class PasswordExpiredModel : BasePasswordExpiredModel
{
    public PasswordExpiredModel(
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager
    ) : base(userManager, signInManager) { }
}
