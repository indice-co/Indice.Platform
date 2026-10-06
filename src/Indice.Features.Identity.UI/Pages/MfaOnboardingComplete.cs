using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the MFA onboarding completion screen.</summary>
/// <remarks>
/// Intentionally does not use <see cref="Indice.Features.Identity.UI.Filters.UserActivityRequirementFilter{TUser}"/>:
/// once an MFA method is enabled, the MFA onboarding requirement is already satisfied, so the filter
/// would auto sign-in the user and redirect before this page could be shown.
/// </remarks>
[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.ExtendedValidationScheme)]
[IdentityUI(typeof(MfaOnboardingCompleteModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseMfaOnboardingCompleteModel : BasePageModel
{
    /// <summary>Key used for setting and retrieving temp data.</summary>
    public static string TempDataKey => "mfa_onboarding_complete";

    /// <summary>MFA onboarding complete view model.</summary>
    public MfaOnboardingCompleteViewModel View { get; set; } = new MfaOnboardingCompleteViewModel();

    /// <summary>MFA onboarding complete page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual IActionResult OnGet([FromQuery] string? returnUrl) {
        var tempModel = TempData.Peek<MfaOnboardingCompleteViewModel>(TempDataKey);
        if (tempModel is null) {
            return RedirectToPage("/MfaOnboarding", routeValues: new { returnUrl });
        }
        View = tempModel;
        View.ReturnUrl = SanitizeReturnUrl(View.ReturnUrl ?? returnUrl);
        return Page();
    }

    /// <summary>MFA onboarding complete page POST handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual IActionResult OnPost([FromQuery] string? returnUrl) {
        var tempModel = TempData.Peek<MfaOnboardingCompleteViewModel>(TempDataKey);
        TempData.Remove(TempDataKey);
        var targetReturnUrl = tempModel?.ReturnUrl ?? returnUrl;
        return RedirectToPage("/MfaOnboarding", routeValues: new { returnUrl = targetReturnUrl });
    }
}

internal class MfaOnboardingCompleteModel : BaseMfaOnboardingCompleteModel
{
}
