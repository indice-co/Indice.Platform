using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the MFA onboarding recovery codes screen.</summary>
/// <remarks>
/// Intentionally does not use <see cref="Indice.Features.Identity.UI.Filters.UserActivityRequirementFilter{TUser}"/>:
/// once the authenticator app is verified, the MFA onboarding requirement is already satisfied, so the filter
/// would auto sign-in the user and redirect before the codes could be shown.
/// </remarks>
[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.ExtendedValidationScheme)]
[IdentityUI(typeof(MfaOnboardingRecoveryCodesModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseMfaOnboardingRecoveryCodesModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseMfaOnboardingRecoveryCodesModel"/> class.</summary>
    public BaseMfaOnboardingRecoveryCodesModel() { }

    /// <summary>MFA onboarding recovery codes view model.</summary>
    public RecoveryCodesViewModel View { get; set; } = new RecoveryCodesViewModel();

    /// <summary>MFA onboarding recovery codes page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual IActionResult OnGet([FromQuery] string? returnUrl) {
        var tempModel = TempData.Peek<RecoveryCodesViewModel>(BaseMfaOnboardingSetupAuthenticatorModel.RecoveryCodesTempDataKey);
        if (tempModel is null || tempModel.RecoveryCodes is null || tempModel.RecoveryCodes.Length == 0) {
            return RedirectToPage("/MfaOnboarding", routeValues: new { returnUrl });
        }
        View = tempModel;
        View.ReturnUrl ??= returnUrl;
        return Page();
    }

    /// <summary>MFA onboarding recovery codes page POST handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual IActionResult OnPost([FromQuery] string? returnUrl) {
        var tempModel = TempData.Peek<RecoveryCodesViewModel>(BaseMfaOnboardingSetupAuthenticatorModel.RecoveryCodesTempDataKey);
        TempData.Remove(BaseMfaOnboardingSetupAuthenticatorModel.RecoveryCodesTempDataKey);
        var targetReturnUrl = tempModel?.ReturnUrl ?? returnUrl;
        return RedirectToPage("/MfaOnboarding", routeValues: new { returnUrl = targetReturnUrl });
    }
}

internal class MfaOnboardingRecoveryCodesModel : BaseMfaOnboardingRecoveryCodesModel
{
    public MfaOnboardingRecoveryCodesModel() : base() { }
}
