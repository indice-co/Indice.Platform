using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the forgot password confirmation screen.</summary>
[AllowAnonymous]
[IdentityUI(typeof(ForgotPasswordConfirmationModel))]
[SecurityHeaders]
public abstract class BaseForgotPasswordConfirmationModel : BasePageModel
{
    private readonly IStringLocalizer<BaseForgotPasswordConfirmationModel> _localizer;

    /// <summary>Creates a new instance of <see cref="BaseForgotPasswordConfirmationModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseForgotPasswordConfirmationModel(
        ExtendedUserManager<User> userManager,
        ILogger<BaseForgotPasswordConfirmationModel> logger,
        IStringLocalizer<BaseForgotPasswordConfirmationModel> localizer
    ) : base() {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _localizer = localizer;
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>Represents a type used to perform logging.</summary>
    protected ILogger<BaseForgotPasswordConfirmationModel> Logger { get; }

    /// <summary>Forgot password confirmation input model data.</summary>
    [BindProperty]
    public ForgotPasswordConfirmationInputModel Input { get; set; } = new ForgotPasswordConfirmationInputModel();

    /// <summary>View model for forgot password confirmation model.</summary>
    public ForgotPasswordConfirmationViewModel View { get; set; } = new ForgotPasswordConfirmationViewModel();

    /// <summary></summary>
    [ViewData]
    public bool PasswordSuccessfullyChanged { get; set; }

    /// <summary>Forgot password confirmation page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync([FromQuery] string email, [FromQuery] string token) {
        Input.Email = email;
        Input.Token = token;
        if (!string.IsNullOrWhiteSpace(email)) {
            var user = await UserManager.FindByEmailAsync(email);
            if (user is not null) {
                View.UserId = user.Id;
                View.UserName = user.UserName;
            }
        }
        return Page();
    }

    /// <summary>Forgot password confirmation page POST handler.</summary>
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> OnPostAsync() {
        if (!ModelState.IsValid) {
            return Page();
        }
        var user = await UserManager.FindByEmailAsync(Input.Email ?? throw new InvalidOperationException("Email cannot be null."));
        if (user is null) {
            // Don't inform the user what went wrong! This is for security reasons.
            ModelState.AddModelError(string.Empty, _localizer["Something went wrong."]);
            return Page();
        }
        var result = await UserManager.ResetPasswordAsync(user, Input.Token, Input.NewPassword);
        if (!result.Succeeded) {
            AddModelErrors(result);
            return Page();
        }
        PasswordSuccessfullyChanged = true;
        Input.Token = Input.NewPassword = null;
        return Page();
    }
}

internal class ForgotPasswordConfirmationModel : BaseForgotPasswordConfirmationModel
{
    public ForgotPasswordConfirmationModel(
        ExtendedUserManager<User> userManager,
        ILogger<ForgotPasswordConfirmationModel> logger,
        IStringLocalizer<ForgotPasswordConfirmationModel> localizer
    ) : base(userManager, logger, localizer) { }
}
