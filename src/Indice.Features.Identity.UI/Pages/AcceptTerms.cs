using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Indice.Features.Identity.UI.Models;
using Indice.Security;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the extended validation add email screen.</summary>
[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.ExtendedValidationScheme)]
[UserActivityRequirementFilter<User>(UserActivityRequirementKind.RequiresAcceptanceOfTerms)]
[IdentityUI(typeof(AcceptTermsModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseAcceptTermsModel(ExtendedUserManager<User> userManager) : BasePageModel
{
    /// <summary>The view model that backs the accept terms page.</summary>
    public AcceptTermsViewModel View { get; set; } = null!;

    /// <summary>The user manager that provides the APIs for managing users and their related data in a persistence store.</summary>
    public ExtendedUserManager<User> UserManager { get; } = userManager ?? throw new ArgumentNullException(nameof(userManager));

    /// <summary>Extended validation accept terms and conditions page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnGetAsync([FromQuery] string? returnUrl) {
        View = await BuildAcceptTermsViewModelAsync(returnUrl);
        return Page();
    }

    /// <summary>Extended validation accept terms and conditions page POST handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnPostAsync([FromQuery] string? returnUrl) {
        if (!ModelState.IsValid) {
            View = await BuildAcceptTermsViewModelAsync(returnUrl);
            return Page();
        }
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        await UserManager.ReplaceClaimAsync(user, BasicClaimTypes.ConsentTerms, bool.TrueString.ToLower());
        await UserManager.ReplaceClaimAsync(user, BasicClaimTypes.ConsentTermsDate, $"{DateTime.UtcNow:O}");
        return Page();
    }

    private async Task<AcceptTermsViewModel> BuildAcceptTermsViewModelAsync(string? returnUrl) {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        var claims = await UserManager.GetClaimsAsync(user);
        var consent = claims.SingleOrDefault(x => x.Type == BasicClaimTypes.ConsentTerms)?.Value;
        var consentDateText = claims.SingleOrDefault(x => x.Type == BasicClaimTypes.ConsentTermsDate)?.Value;
        var consentDate = new DateTime?();
        if (consentDateText != null && DateTime.TryParse(consentDateText, out var date)) {
            consentDate = date;
        }
        return new() {
            Alert = AlertModel.Info("Please read and accept the terms and conditions to continue."),
            LastConsentDate = consentDate,
            LastConsent = bool.TrueString.Equals(consent, StringComparison.OrdinalIgnoreCase),
            LastUpdateDate = DateTimeOffset.UtcNow,
            ReturnUrl = returnUrl,
        };
    }
}

[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.ExtendedValidationScheme)]
internal class AcceptTermsModel(ExtendedUserManager<User> userManager) : BaseAcceptTermsModel(userManager)
{
}