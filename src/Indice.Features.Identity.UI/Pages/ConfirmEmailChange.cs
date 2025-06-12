using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the confirm email page screen.</summary>
[IdentityUI(typeof(ConfirmEmailChangeModel))]
[SecurityHeaders]
public abstract class BaseConfirmEmailChangeModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseConfirmEmailChangeModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseConfirmEmailChangeModel(
        ExtendedUserManager<User> userManager) : base() {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }
    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }

    /// <summary>Input model that will bind either on GET via querystring or on POST.</summary>
    [BindProperty(SupportsGet = true)]
    public ConfirmEmailChangeInputModel Input { get; set; } = new();
    /// <summary>View model</summary>
    public ConfirmEmailViewModel View { get; set; } = new();

    /// <summary>Confirm email page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        var user = await UserManager.FindByIdAsync(Input.UserId);
        if (user is null) {
            ModelState.AddModelError(string.Empty, "user not found");
            return Page();
        }
        if (!string.IsNullOrWhiteSpace(Input.ReturnUrl) && !string.IsNullOrWhiteSpace(Input.ClientId)) {
            View.ReturnUrl = QueryHelpers.AddQueryString(Input.ReturnUrl, "client_id", Input.ClientId);
        }
        View.Email = Input.Email;
        View.AlreadyVerified = (user.Email == Input.Email) && await UserManager.IsEmailConfirmedAsync(user);
        return Page();
    }

    /// <summary>Confirm email page GET handler.</summary>
    public virtual async Task<IActionResult> OnPostAsync() {
        var user = await UserManager.FindByIdAsync(Input.UserId);
        if (user is null) {
            ModelState.AddModelError(string.Empty, "user not found");
            return Page();
        }
        if (!string.IsNullOrWhiteSpace(Input.ReturnUrl) && !string.IsNullOrWhiteSpace(Input.ClientId)) {
            View.ReturnUrl = QueryHelpers.AddQueryString(Input.ReturnUrl, "client_id", Input.ClientId);
        }
        View.AlreadyVerified = (user.Email == Input.Email) && await UserManager.IsEmailConfirmedAsync(user);
        if (!View.AlreadyVerified) {
            var result = await UserManager.ChangeEmailAsync(user, Input.Email, Input.Token);
            View.Verified = result.Succeeded;
            View.InvalidOrExpiredToken = !result.Succeeded;
            if (View.InvalidOrExpiredToken) {
                return Page();
            }
            if (UserManager.EmailAsUserName) {
                await UserManager.SetUserNameAsync(user, Input.Email);
            }
        }
        if (Input.ShouldRedirect && !string.IsNullOrWhiteSpace(Input.ReturnUrl) && IsValidReturnUrl(Input.ReturnUrl)) {
            return Redirect(Input.ReturnUrl!);
        }
        return Page();
    }

    /// <summary>Input model for confirming an email change request.</summary>
    public class ConfirmEmailChangeInputModel : ConfirmEmailInputModel
    {
        /// <summary>The new email address that the user is trying to confirm.</summary>
        public string Email { get; set; } = null!;
    }
}

internal class ConfirmEmailChangeModel : BaseConfirmEmailChangeModel
{
    public ConfirmEmailChangeModel(
        ExtendedUserManager<User> userManager
    ) : base(userManager) { }
}
