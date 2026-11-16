using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the change username screen. When the user has MFA enabled, the change requires an MFA challenge.</summary>
/// <remarks>The page is not available when <see cref="IdentityUIOptions.DisableEmailEdit"/> is set or when the email is used as the username.</remarks>
[Authorize]
[IdentityUI(typeof(ChangeUserNameModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseChangeUserNameModel : BaseProfileMfaChallengePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseChangeUserNameModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="identityUiOptions">Configuration options for Identity UI.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseChangeUserNameModel(
        ExtendedUserManager<User> userManager,
        IOptions<IdentityUIOptions> identityUiOptions
    ) : base(userManager) {
        IdentityUiOptions = identityUiOptions?.Value ?? throw new ArgumentNullException(nameof(identityUiOptions));
    }

    /// <summary>Configuration options for Identity UI.</summary>
    public IdentityUIOptions IdentityUiOptions { get; }

    /// <summary>The input model that backs the change username page.</summary>
    [BindProperty]
    public ChangeUserNameInputModel Input { get; set; } = new ChangeUserNameInputModel();

    /// <summary>The current username of the user.</summary>
    public string? CurrentUserName { get; set; }

    /// <inheritdoc />
    protected override bool IsEditDisabled => IdentityUiOptions.DisableEmailEdit || UserManager.EmailAsUserName;

    /// <inheritdoc />
    protected override Task PreparePageAsync(User user) {
        CurrentUserName = user.UserName;
        return Task.CompletedTask;
    }

    /// <summary>Change username page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        if (IsEditDisabled) {
            return RedirectToPage("/Profile");
        }
        var user = await GetUserAsync();
        await PreparePageAsync(user);
        await EnsureMfaChallengeAsync(user);
        return Page();
    }

    /// <summary>Change username page POST handler.</summary>
    public virtual async Task<IActionResult> OnPostAsync() {
        if (IsEditDisabled) {
            return BadRequest();
        }
        var user = await GetUserAsync();
        await PreparePageAsync(user);
        if (!await EnsureMfaChallengeAsync(user)) {
            ModelState.Clear();
            return Page();
        }
        if (!ModelState.IsValid) {
            return Page();
        }
        var newUserName = Input.UserName!.Trim();
        if (string.Equals(user.UserName, newUserName, StringComparison.Ordinal)) {
            return RedirectToPage("/Profile");
        }
        var result = await UserManager.SetUserNameAsync(user, newUserName);
        if (!result.Succeeded) {
            foreach (var error in result.Errors) {
                ModelState.AddModelError($"{nameof(Input)}.{nameof(ChangeUserNameInputModel.UserName)}", error.Description);
            }
            return Page();
        }
        var signInManager = HttpContext.RequestServices.GetRequiredService<ExtendedSignInManager<User>>();
        await signInManager.RefreshSignInAsync(user);
        TempData.Put("Alert", AlertModel.Success(IdentityLabels.ChangeUserName_Succeeded));
        return RedirectToPage("/Profile");
    }
}

internal class ChangeUserNameModel : BaseChangeUserNameModel
{
    public ChangeUserNameModel(
        ExtendedUserManager<User> userManager,
        IOptions<IdentityUIOptions> identityUiOptions
    ) : base(userManager, identityUiOptions) { }
}
