using IdentityServer4.Services;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using static IdentityServer4.Models.IdentityResources;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the confirm email page screen.</summary>
[IdentityUI(typeof(ConfirmEmailModel))]
[SecurityHeaders]
public abstract class BaseConfirmEmailModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseConfirmEmailModel"/> class.</summary>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="BaseConfirmEmailModel"/>.</param>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseConfirmEmailModel(
        IStringLocalizer<BaseConfirmEmailModel> localizer,
        ExtendedUserManager<User> userManager) : base() {
        Localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }
    /// <summary>Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="BaseConfirmEmailModel"/>.</summary>
    protected IStringLocalizer<BaseConfirmEmailModel> Localizer { get; }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }

    /// <summary>Input model that will bind either on GET via querystring or on POST.</summary>
    [BindProperty(SupportsGet = true)]
    public ConfirmEmailInputModel Input { get; set; } = new();
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
        View.Email = user.Email;
        View.AlreadyVerified = await UserManager.IsEmailConfirmedAsync(user);
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
        View.AlreadyVerified = await UserManager.IsEmailConfirmedAsync(user);
        if (!View.AlreadyVerified) {
            var result = await UserManager.ConfirmEmailAsync(user, Input.Token);
            View.Verified = result.Succeeded;
            View.InvalidOrExpiredToken = !result.Succeeded;
            if (View.InvalidOrExpiredToken) {
                return Page();
            }
        }
        if (Input.ShouldRedirect && !string.IsNullOrWhiteSpace(Input.ReturnUrl) && IsValidReturnUrl(Input.ReturnUrl)) {
            return Redirect(Input.ReturnUrl!);
        }
        return Page();
    }
}

internal class ConfirmEmailModel : BaseConfirmEmailModel
{
    public ConfirmEmailModel(
        IStringLocalizer<ConfirmEmailModel> localizer,
        ExtendedUserManager<User> userManager
    ) : base(localizer, userManager) { }
}
