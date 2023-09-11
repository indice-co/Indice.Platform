using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the change password screen.</summary>
[Authorize]
[IdentityUI(typeof(ChangePasswordModel))]
[SecurityHeaders]
public abstract class BaseChangePasswordModel : BasePageModel
{
    private readonly IStringLocalizer<BaseChangePasswordModel> _localizer;

    /// <summary>Creates a new instance of <see cref="BaseChangePasswordModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseChangePasswordModel(
        ExtendedUserManager<User> userManager,
        ILogger<BaseChangePasswordModel> logger,
        IStringLocalizer<BaseChangePasswordModel> localizer
    ) {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>Represents a type used to perform logging.</summary>
    protected ILogger<BaseChangePasswordModel> Logger { get; }

    /// <summary>Forgot password input model data.</summary>
    [BindProperty]
    public ChangePasswordInputModel Input { get; set; } = new ChangePasswordInputModel();

    /// <summary>Determines whether the request is sent once.</summary>
    [ViewData]
    public bool PasswordSuccessfullyChanged { get; set; }

    /// <summary>Change password page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        if (!await UserManager.HasPasswordAsync(user)) {
            return RedirectToPage("/AddPassword");
        }
        return Page();
    }

    /// <summary>Change password page POST handler.</summary>
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> OnPostAsync() {
        if (!ModelState.IsValid) {
            return Page();
        }
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        var result = await UserManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
        if (!result.Succeeded) {
            AddModelErrors(result);
            return Page();
        }
        PasswordSuccessfullyChanged = true;
        return Page();
    }
}

internal class ChangePasswordModel : BaseChangePasswordModel
{
    public ChangePasswordModel(
        ExtendedUserManager<User> userManager,
        ILogger<ChangePasswordModel> logger,
        IStringLocalizer<ChangePasswordModel> localizer
    ) : base(userManager, logger, localizer) { }
}
