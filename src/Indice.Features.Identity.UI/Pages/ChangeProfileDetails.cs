#if NET9_0_OR_GREATER
using Duende.IdentityModel;
#else
using IdentityModel;
#endif
using System.Globalization;
using System.Security.Claims;
using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the edit profile details screen.</summary>
/// <remarks>
/// Besides the well known profile fields, the page lists every claim type that is marked as user editable
/// (<see cref="ClaimType.UserEditable"/>) and is not auto generated or managed by another page.
/// </remarks>
[Authorize]
[IdentityUI(typeof(ChangeProfileDetailsModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseChangeProfileDetailsModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseChangeProfileDetailsModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="configurationDbContext">Extended DbContext for the IdentityServer configuration data.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseChangeProfileDetailsModel(
        ExtendedUserManager<User> userManager,
        ExtendedConfigurationDbContext configurationDbContext
    ) : base() {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        ConfigurationDbContext = configurationDbContext ?? throw new ArgumentNullException(nameof(configurationDbContext));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>Extended DbContext for the IdentityServer configuration data.</summary>
    protected ExtendedConfigurationDbContext ConfigurationDbContext { get; }

    /// <summary>The input model that backs the edit profile details page.</summary>
    [BindProperty]
    public ProfileDetailsInputModel Input { get; set; } = new ProfileDetailsInputModel();

    /// <summary>Edit profile details page view model.</summary>
    public ProfileDetailsViewModel View { get; set; } = new ProfileDetailsViewModel();

    /// <summary>Edit profile details page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        var user = await GetUserAsync();
        var claims = await UserManager.GetClaimsAsync(user);
        View = await BuildViewModelAsync(claims);
        Input = new ProfileDetailsInputModel {
            FirstName = FindClaimValue(claims, JwtClaimTypes.GivenName),
            LastName = FindClaimValue(claims, JwtClaimTypes.FamilyName),
            Tin = FindClaimValue(claims, BasicClaimTypes.Tin),
            BirthDate = ParseBirthDate(FindClaimValue(claims, JwtClaimTypes.BirthDate)),
            ZoneInfo = FindClaimValue(claims, JwtClaimTypes.ZoneInfo),
            ConsentCommercial = IsConsentGiven(claims),
            Claims = View.Claims.Select(x => new ProfileClaimInputModel { Type = x.Type, Value = x.Value }).ToList()
        };
        return Page();
    }

    /// <summary>Edit profile details page POST handler.</summary>
    public virtual async Task<IActionResult> OnPostAsync() {
        var user = await GetUserAsync();
        var claims = await UserManager.GetClaimsAsync(user);
        View = await BuildViewModelAsync(claims);
        // Read-only fields keep their stored values, whatever was posted.
        if (View.DisableEditGivenName) {
            Input.FirstName = FindClaimValue(claims, JwtClaimTypes.GivenName);
            ModelState.Remove($"{nameof(Input)}.{nameof(ProfileDetailsInputModel.FirstName)}");
        }
        if (View.DisableEditFamilyName) {
            Input.LastName = FindClaimValue(claims, JwtClaimTypes.FamilyName);
            ModelState.Remove($"{nameof(Input)}.{nameof(ProfileDetailsInputModel.LastName)}");
        }
        if (View.DisableEditTin) {
            Input.Tin = FindClaimValue(claims, BasicClaimTypes.Tin);
            ModelState.Remove($"{nameof(Input)}.{nameof(ProfileDetailsInputModel.Tin)}");
        }
        var claimChanges = ValidateClaims();
        if (!string.IsNullOrWhiteSpace(Input.ZoneInfo)) {
            var zoneInfoProvider = ServiceProvider.GetService<IZoneInfoProvider>();
            if (zoneInfoProvider is not null && !zoneInfoProvider.GetTimeZones().Any(x => x.Id == Input.ZoneInfo)) {
                ModelState.AddModelError($"{nameof(Input)}.{nameof(ProfileDetailsInputModel.ZoneInfo)}", string.Format(CultureInfo.CurrentUICulture, IdentityLabels.Profile_InvalidValue, IdentityLabels.Profile_Timezone));
            }
        }
        if (!ModelState.IsValid) {
            return Page();
        }

        if (!View.DisableEditGivenName) {
            await ReplaceClaimIfChangedAsync(user, claims, JwtClaimTypes.GivenName, Input.FirstName?.Trim());
        }
        if (!View.DisableEditFamilyName) {
            await ReplaceClaimIfChangedAsync(user, claims, JwtClaimTypes.FamilyName, Input.LastName?.Trim());
        }
        if (!View.DisableEditTin) {
            await ReplaceClaimIfChangedAsync(user, claims, BasicClaimTypes.Tin, Input.Tin?.Trim());
        }
        var birthDate = Input.BirthDate?.ToString(ProfileClaims.DateFormat, CultureInfo.InvariantCulture);
        if (birthDate != ParseBirthDate(FindClaimValue(claims, JwtClaimTypes.BirthDate))?.ToString(ProfileClaims.DateFormat, CultureInfo.InvariantCulture)) {
            AddModelErrors(await UserManager.ReplaceClaimAsync(user, JwtClaimTypes.BirthDate, birthDate ?? string.Empty));
        }
        if (Input.ConsentCommercial != IsConsentGiven(claims)) {
            AddModelErrors(await UserManager.ReplaceClaimAsync(user, BasicClaimTypes.ConsentCommercial, Input.ConsentCommercial ? "true" : "false"));
            AddModelErrors(await UserManager.ReplaceClaimAsync(user, BasicClaimTypes.ConsentCommercialDate, $"{DateTime.UtcNow:O}"));
        }
        if (!string.IsNullOrWhiteSpace(Input.ZoneInfo)) {
            await ReplaceClaimIfChangedAsync(user, claims, JwtClaimTypes.ZoneInfo, Input.ZoneInfo);
        }
        foreach (var (type, value) in claimChanges) {
            AddModelErrors(await UserManager.ReplaceClaimAsync(user, type, value));
        }
        if (!ModelState.IsValid) {
            return Page();
        }
        var signInManager = ServiceProvider.GetRequiredService<ExtendedSignInManager<User>>();
        await signInManager.RefreshSignInAsync(user);
        TempData.Put("Alert", AlertModel.Success(IdentityLabels.Profile_ProfileUpdatedSuccessfully));
        return RedirectToPage("/Profile");
    }

    /// <summary>Gets the signed in user.</summary>
    protected async Task<User> GetUserAsync() => await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");

    // Aligns the posted claims with the user editable claims (unknown types are ignored) and validates the changed values.
    private List<(string Type, string Value)> ValidateClaims() {
        var posted = (Input.Claims ?? [])
            .Where(x => !string.IsNullOrEmpty(x.Type))
            .GroupBy(x => x.Type!, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.First().Value, StringComparer.Ordinal);
        foreach (var key in ModelState.Keys.Where(x => x.StartsWith($"{nameof(Input)}.{nameof(ProfileDetailsInputModel.Claims)}", StringComparison.Ordinal)).ToList()) {
            ModelState.Remove(key);
        }
        var changes = new List<(string Type, string Value)>();
        Input.Claims = [];
        for (var i = 0; i < View.Claims.Count; i++) {
            var claim = View.Claims[i];
            if (!posted.TryGetValue(claim.Type, out var postedValue)) {
                Input.Claims.Add(new ProfileClaimInputModel { Type = claim.Type, Value = claim.Value });
                continue;
            }
            Input.Claims.Add(new ProfileClaimInputModel { Type = claim.Type, Value = postedValue });
            if (!ProfileClaims.HasChanged(claim, postedValue)) {
                continue;
            }
            var error = ProfileClaims.TryNormalize(claim, postedValue, out var value);
            if (error is null) {
                changes.Add((claim.Type, value));
                continue;
            }
            var message = error == ProfileClaimError.Required ? IdentityLabels.Profile_RequiredValue : IdentityLabels.Profile_InvalidValue;
            ModelState.AddModelError($"{nameof(Input)}.{nameof(ProfileDetailsInputModel.Claims)}[{i}].{nameof(ProfileClaimInputModel.Value)}", string.Format(CultureInfo.CurrentUICulture, message, claim.DisplayName));
        }
        return changes;
    }

    private async Task<ProfileDetailsViewModel> BuildViewModelAsync(IList<Claim> claims) {
        var userEditableClaimTypes = await ProfileClaims.GetUserEditableClaimTypesAsync(ConfigurationDbContext);
        return new ProfileDetailsViewModel {
            DisableEditGivenName = !userEditableClaimTypes.Any(x => x.Name == JwtClaimTypes.GivenName),
            DisableEditFamilyName = !userEditableClaimTypes.Any(x => x.Name == JwtClaimTypes.FamilyName),
            DisableEditTin = !userEditableClaimTypes.Any(x => x.Name == BasicClaimTypes.Tin),
            Claims = ProfileClaims.GetAdditionalClaims(userEditableClaimTypes, claims)
        };
    }

    private async Task ReplaceClaimIfChangedAsync(User user, IList<Claim> claims, string claimType, string? value) {
        if (string.Equals(FindClaimValue(claims, claimType) ?? string.Empty, value ?? string.Empty, StringComparison.Ordinal)) {
            return;
        }
        AddModelErrors(await UserManager.ReplaceClaimAsync(user, claimType, value ?? string.Empty));
    }

    private static string? FindClaimValue(IEnumerable<Claim> claims, string claimType) => claims.FirstOrDefault(x => x.Type == claimType)?.Value;

    private static bool IsConsentGiven(IEnumerable<Claim> claims) => FindClaimValue(claims, BasicClaimTypes.ConsentCommercial) == bool.TrueString.ToLower();

    private static DateTime? ParseBirthDate(string? value) => value is not null && DateTime.TryParse(value, out var date) ? date : null;
}

internal class ChangeProfileDetailsModel : BaseChangeProfileDetailsModel
{
    public ChangeProfileDetailsModel(
        ExtendedUserManager<User> userManager,
        ExtendedConfigurationDbContext configurationDbContext
    ) : base(userManager, configurationDbContext) { }
}
