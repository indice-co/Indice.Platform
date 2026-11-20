#if NET9_0_OR_GREATER
using Duende.IdentityModel;
#else
using IdentityModel;
#endif
using System.Globalization;
using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the edit preferences screen (preferred language).</summary>
/// <remarks>The page is not available when no supported cultures are configured in <see cref="RequestLocalizationOptions"/>.</remarks>
[Authorize]
[IdentityUI(typeof(ChangePreferencesModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseChangePreferencesModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseChangePreferencesModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="localizationOptions">The request localization options.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseChangePreferencesModel(
        ExtendedUserManager<User> userManager,
        IOptions<RequestLocalizationOptions> localizationOptions
    ) : base() {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        SupportedCultures = localizationOptions?.Value?.SupportedCultures ?? [];
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }

    /// <summary>List of available language preferences.</summary>
    public IList<CultureInfo> SupportedCultures { get; }

    /// <summary>The input model that backs the edit preferences page.</summary>
    [BindProperty]
    public ProfileLanguagePreferenceInputModel Input { get; set; } = new ProfileLanguagePreferenceInputModel();

    /// <summary>Edit preferences page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        if (SupportedCultures.Count == 0) {
            return RedirectToPage("/Profile");
        }
        var user = await GetUserAsync();
        var claims = await UserManager.GetClaimsAsync(user);
        Input.Locale = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Locale)?.Value;
        return Page();
    }

    /// <summary>Edit preferences page POST handler.</summary>
    public virtual async Task<IActionResult> OnPostAsync() {
        if (SupportedCultures.Count == 0) {
            return BadRequest();
        }
        var user = await GetUserAsync();
        var locale = Input.Locale?.Trim();
        var localeKey = $"{nameof(Input)}.{nameof(ProfileLanguagePreferenceInputModel.Locale)}";
        var culture = string.IsNullOrEmpty(locale) ? null : SupportedCultures.FirstOrDefault(x => string.Equals(x.TwoLetterISOLanguageName, locale, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(locale)) {
            ModelState.AddModelError(localeKey, string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_RequiredValue, IdentityLabels.Profile_PreferredLanguage));
        } else if (culture is null) {
            ModelState.AddModelError(localeKey, string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_InvalidValue, IdentityLabels.Profile_PreferredLanguage));
        }
        if (!ModelState.IsValid) {
            return Page();
        }
        var result = await UserManager.ReplaceClaimAsync(user, JwtClaimTypes.Locale, culture!.TwoLetterISOLanguageName);
        if (!result.Succeeded) {
            AddModelErrors(result);
            return Page();
        }
        var signInManager = ServiceProvider.GetRequiredService<ExtendedSignInManager<User>>();
        await signInManager.RefreshSignInAsync(user);
        TempData.Put("Alert", AlertModel.Success(IdentityLabels.ChangePreferences_Succeeded));
        return RedirectToPage("/Profile");
    }

    /// <summary>Gets the signed in user.</summary>
    protected async Task<User> GetUserAsync() => await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
}

internal class ChangePreferencesModel : BaseChangePreferencesModel
{
    public ChangePreferencesModel(
        ExtendedUserManager<User> userManager,
        IOptions<RequestLocalizationOptions> localizationOptions
    ) : base(userManager, localizationOptions) { }
}
