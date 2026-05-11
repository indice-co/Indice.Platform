using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

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
    public BaseMfaOnboardingRecoveryCodesModel(IdentityUILocalizer localizer, IConfiguration configuration) {
        Localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        Configuration = configuration;
    }

    /// <summary>MFA onboarding recovery codes view model.</summary>
    public RecoveryCodesViewModel View { get; set; } = new RecoveryCodesViewModel();

    /// <summary>Provides localized messages for identity operations.</summary>
    protected IdentityUILocalizer Localizer { get; }
    /// <summary>Provides access to the application configuration.</summary>
    protected IConfiguration Configuration { get; }

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


    /// <summary>MFA onboarding recovery codes page download Get handler.</summary>
    public virtual async Task<IActionResult> OnGetDownloadAsync() {
        var tempModel = TempData.Peek<RecoveryCodesViewModel>(BaseMfaOnboardingSetupAuthenticatorModel.RecoveryCodesTempDataKey);
        if (tempModel is null || tempModel.RecoveryCodes is null || tempModel.RecoveryCodes.Length == 0) {
            return File(System.Text.Encoding.UTF8.GetBytes("Invalid request"), "text/plain", "recovery-codes.txt");
        }
        var txt = tempModel.ToString(Localizer.ApplicationName(Configuration.GetApplicationName()!), Localizer.MfaOnBoardingRecoveryCodes_FileHeader(tempModel.UserName!).Value);
        return File(System.Text.Encoding.UTF8.GetBytes(txt), "text/plain", "recovery-codes.txt");
    }
}

internal class MfaOnboardingRecoveryCodesModel : BaseMfaOnboardingRecoveryCodesModel
{
    public MfaOnboardingRecoveryCodesModel(IdentityUILocalizer localizer, IConfiguration configuration) : base(localizer, configuration) { }
}
