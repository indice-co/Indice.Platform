using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the add password screen.</summary>
[Authorize]
[IdentityUI(typeof(AddPasswordModel))]
[SecurityHeaders]
public abstract class BaseAddPasswordModel : BasePageModel
{
    private readonly IStringLocalizer<BaseAddPasswordModel> _localizer;

    /// <summary>Creates a new instance of <see cref="BaseAddPasswordModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="signInManager">Provides the APIs for user sign in.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseAddPasswordModel(
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        ILogger<BaseAddPasswordModel> logger,
        IStringLocalizer<BaseAddPasswordModel> localizer
    ) {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        SignInManager = signInManager;
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>Provides the APIs for user sign in.</summary>
    protected ExtendedSignInManager<User> SignInManager { get; }

    /// <summary>Represents a type used to perform logging.</summary>
    protected ILogger<BaseAddPasswordModel> Logger { get; }

    /// <summary>Forgot password input model data.</summary>
    [BindProperty]
    public AddPasswordInputModel Input { get; set; } = new AddPasswordInputModel();

    /// <summary>Determines whether the request is sent once.</summary>
    [ViewData]
    public bool PasswordSuccessfullyAdded { get; set; }

    /// <summary>Add password page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        if (await UserManager.HasPasswordAsync(user)) {
            return RedirectToPage("/ChangePassword");
        }
        return Page();
    }

    /// <summary>Add password page POST handler.</summary>
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> OnPostAsync() {
        if (!ModelState.IsValid) {
            return Page();
        }
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        var addPasswordResult = await UserManager.AddPasswordAsync(user, Input.NewPassword!);
        if (!addPasswordResult.Succeeded) {
            AddModelErrors(addPasswordResult);
            return Page();
        }
        await SignInManager.RefreshSignInAsync(user);
        PasswordSuccessfullyAdded = true;
        return Page();
    }
}

internal class AddPasswordModel : BaseAddPasswordModel
{
    public AddPasswordModel(
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        ILogger<BaseAddPasswordModel> logger,
        IStringLocalizer<BaseAddPasswordModel> localizer
    ) : base(userManager, signInManager, logger, localizer) { }
}
