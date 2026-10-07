using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Filters;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the MFA onboarding add email screen.</summary>
[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.ExtendedValidationScheme)]
[UserActivityRequirementFilter<User>(UserActivityRequirementKind.RequiresMfaOnboarding)]
[IdentityUI(typeof(MfaOnboardingAddEmailModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseMfaOnboardingAddEmailModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseMfaOnboardingAddEmailModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseMfaOnboardingAddEmailModel(
        ExtendedUserManager<User> userManager
    ) {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }

    /// <summary>MFA onboarding add email view model.</summary>
    public EnableMfaEmailViewModel View { get; set; } = new EnableMfaEmailViewModel();

    /// <summary>The input model that backs the MFA onboarding add email page.</summary>
    [BindProperty]
    public EnableMfaEmailInputModel Input { get; set; } = new EnableMfaEmailInputModel();

    /// <summary>Key used for setting and retrieving temp data.</summary>
    public static string TempDataKey => "mfa_onboarding_add_email_alert";

    /// <summary>MFA onboarding add email page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnGetAsync([FromQuery] string? returnUrl) {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        var alert = user.EmailConfirmed
            ? UserManager.MessageDescriber.MfaAddEmailValidationEmailAlreadyConfirmed
            : UserManager.MessageDescriber.MfaAddEmailValidationEmailEmpty;
        TempData.Put(TempDataKey, AlertModel.Info(alert));
        Input = View = new EnableMfaEmailViewModel {
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            ReturnUrl = returnUrl
        };
        return Page();
    }

    /// <summary>MFA onboarding add email page POST handler.</summary>
    public virtual async Task<IActionResult> OnPostAsync([FromQuery] string? returnUrl) {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        if (!ModelState.IsValid) {
            PopulateView(user, returnUrl);
            return Page();
        }
        if (!user.EmailConfirmed) {
            var result = await UserManager.SetEmailAsync(user, Input.Email);
            if (!result.Succeeded) {
                AddModelErrors(result);
                PopulateView(user, returnUrl);
                return Page();
            }
        }
        // A confirmed address is not a second factor for this sign-in. Always send a code
        // and enable MFA only after MfaOnboardingVerifyEmail accepts it.
        if (!await SendVerificationEmailAsync(user)) {
            PopulateView(user, returnUrl);
            ModelState.AddModelError(string.Empty, UserManager.MessageDescriber.LimitAttemptsReached);
            return Page();
        }
        return RedirectToPage("/MfaOnboardingVerifyEmail", routeValues: new { returnUrl = Input.ReturnUrl ?? returnUrl });
    }

    /// <summary>
    /// The POST view model starts empty. Confirmed addresses stay read-only, matching the value this handler actually uses.
    /// </summary>
    private void PopulateView(User user, string? returnUrl) {
        if (user.EmailConfirmed) {
            Input.Email = user.Email;
        }
        View.Email = Input.Email;
        View.EmailConfirmed = user.EmailConfirmed;
        View.ReturnUrl = Input.ReturnUrl ?? returnUrl;
    }
}

internal class MfaOnboardingAddEmailModel : BaseMfaOnboardingAddEmailModel
{
    public MfaOnboardingAddEmailModel(
        ExtendedUserManager<User> userManager
    ) : base(userManager) { }
}
