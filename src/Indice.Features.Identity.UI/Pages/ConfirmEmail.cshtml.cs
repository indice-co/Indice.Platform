using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

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
        ExtendedUserManager<User> userManager
    ) {
        Localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <summary>Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="BaseConfirmEmailModel"/>.</summary>
    protected IStringLocalizer<BaseConfirmEmailModel> Localizer { get; }
    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>The URL to return to.</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>Confirm email page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync([FromQuery] string? userId, [FromQuery] string? token, [FromQuery] string? returnUrl) {
        ReturnUrl = returnUrl;
        if (string.IsNullOrWhiteSpace(userId)) {
            throw new ArgumentNullException(nameof(userId), "Parameter cannot be null.");
        }
        if (string.IsNullOrWhiteSpace(token)) {
            throw new ArgumentNullException(nameof(token), "Parameter cannot be null.");
        }
        var user = await UserManager.FindByIdAsync(userId);
        if (user is null) {
            ModelState.AddModelError(string.Empty, string.Empty);
            return Page();
        }
        if (await UserManager.IsEmailConfirmedAsync(user)) {
            ModelState.AddModelError(string.Empty, string.Empty);
            return Page();
        }
        var result = await UserManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded) {
            AddModelErrors(result);
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