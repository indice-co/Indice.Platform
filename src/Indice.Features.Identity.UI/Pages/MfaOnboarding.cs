using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the MFA onboarding screen.</summary>
[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.MfaOnboardingScheme)]
[IdentityUI(typeof(MfaOnboardingModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseMfaOnboardingModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseMfaOnboardingModel"/> class.</summary>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseMfaOnboardingModel(IAuthenticationMethodProvider authenticationMethodProvider) {
        AuthenticationMethodProvider = authenticationMethodProvider ?? throw new ArgumentNullException(nameof(authenticationMethodProvider));
    }

    /// <summary>Abstracts interaction with system's various authentication methods.</summary>
    protected IAuthenticationMethodProvider AuthenticationMethodProvider { get; }

    /// <summary>MFA onboarding Login view model.</summary>
    public MfaOnboardingViewModel View { get; set; } = new MfaOnboardingViewModel();

    /// <summary>The input model that backs the MFA onboarding page.</summary>
    [BindProperty]
    public MfaOnboardingInputModel Input { get; set; } = new MfaOnboardingInputModel();

    /// <summary>Key used for setting and retrieving temp data.</summary>
    public static string TempDataKey => "mfa_onboarding_info_message";

    /// <summary>MFA onboarding page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnGetAsync([FromQuery] string? returnUrl) {
        Input.ReturnUrl = returnUrl;
        View.ReturnUrl = returnUrl;
        View.AuthenticationMethods = await AuthenticationMethodProvider.GetAllMethodsAsync();
        return Page();
    }

    /// <summary>MFA onboarding page POST handler.</summary>
    public virtual async Task<IActionResult> OnPostAsync([FromQuery] string? returnUrl) {
        if (!ModelState.IsValid) {
            View.AuthenticationMethods = await AuthenticationMethodProvider.GetAllMethodsAsync();
            return Page();
        }
        await Task.CompletedTask;
        var redirectUrl = Input.SelectedAuthenticationMethod switch {
            AuthenticationMethodType.PhoneNumber => Url.PageLink("/MfaOnboardingAddPhone", values: new { returnUrl = Input.ReturnUrl }),
            _ => throw new NotImplementedException("Only SMS authentication method as second factor is currently supported."),
        };
        TempData.Put(TempDataKey, new MfaOnboardingTempDataModel {
            SelectedAuthenticationMethod = Input.SelectedAuthenticationMethod.Value
        });
        return Redirect(redirectUrl ?? throw new InvalidOperationException("No URL was generated to redirect."));
    }
}

internal class MfaOnboardingModel : BaseMfaOnboardingModel
{
    public MfaOnboardingModel(IAuthenticationMethodProvider authenticationMethodProvider) : base(authenticationMethodProvider) { }
}
