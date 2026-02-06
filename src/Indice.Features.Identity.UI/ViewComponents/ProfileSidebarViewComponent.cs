#if NET9_0_OR_GREATER
using Duende.IdentityModel;
#else
using IdentityModel;
#endif
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Indice.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.UI.ViewComponents;

/// <summary>
/// Profile Sidebar View component is the area where the user profile actions are presented.
/// </summary>
public class ProfileSidebarViewComponent : ViewComponent
{
    private readonly ExtendedUserManager<User> userManager;
    private readonly ExtendedSignInManager<User> signInManager;
    private readonly IConfiguration configuration;
    private readonly ExtendedConfigurationDbContext configurationDb;

    /// <summary>
    /// Constructs the sidebar view component
    /// </summary>
    /// <param name="userManager"></param>
    /// <param name="signInManager"></param>
    /// <param name="configurationDb"></param>
    /// <param name="configuration"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ProfileSidebarViewComponent(ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        ExtendedConfigurationDbContext configurationDb,
        IConfiguration configuration) {
        this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        this.signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.configurationDb = configurationDb ?? throw new ArgumentNullException(nameof(configurationDb));
    }

    /// <inheritdoc/>
    public async Task<IViewComponentResult> InvokeAsync() {
        var user = await userManager.GetUserAsync((System.Security.Claims.ClaimsPrincipal)User);
        if (user is null) {
            return View(new ProfileViewModel());
        }
        var roles = await userManager.GetRolesAsync(user);
        var claims = await userManager.GetClaimsAsync(user);
        var birthDate = default(DateTime?);
        var birthDateText = claims.SingleOrDefault(x => x.Type == JwtClaimTypes.BirthDate)?.Value;
        if (birthDateText != null && DateTime.TryParse(birthDateText, out var date)) {
            birthDate = date;
        }
        var currentLogins = await userManager.GetLoginsAsync(user);
        var otherLogins = (await signInManager.GetExternalAuthenticationSchemesAsync())
            .Where(scheme => currentLogins.All(loginInfo => scheme.Name != loginInfo.LoginProvider))
            .ToList();
        var consentDateText = claims.SingleOrDefault(x => x.Type == BasicClaimTypes.ConsentCommercialDate)?.Value;
        var consentDate = new DateTime?();
        if (consentDateText != null && DateTime.TryParse(consentDateText, out date)) {
            consentDate = date;
        }
        var porfileEditableClaimsList = await configurationDb.ClaimTypes.Where(x => x.Name == BasicClaimTypes.FamilyName || x.Name == BasicClaimTypes.GivenName || x.Name == BasicClaimTypes.Tin).ToListAsync();
        var canEditTin = porfileEditableClaimsList.FirstOrDefault(x => x.Name == BasicClaimTypes.Tin)?.UserEditable ?? false;
        var canEditFamilyName = porfileEditableClaimsList.FirstOrDefault(x => x.Name == BasicClaimTypes.FamilyName)?.UserEditable ?? false;
        var canEditGivenName = porfileEditableClaimsList.FirstOrDefault(x => x.Name == BasicClaimTypes.GivenName)?.UserEditable ?? false;
        return View(new ProfileViewModel {
            BirthDate = birthDate,
            DisableEditTin = !canEditTin,
            DisableEditFamilyName = !canEditFamilyName,
            DisableEditGivenName = !canEditGivenName,
            CanRemoveProvider = await userManager.HasPasswordAsync(user) || currentLogins.Count > 1,
            ConsentCommercial = claims.SingleOrDefault(x => x.Type == BasicClaimTypes.ConsentCommercial)?.Value == bool.TrueString.ToLower(),
            ConsentCommercialDate = consentDate,
            CurrentLogins = currentLogins,
            Email = user.Email,
            EmailChangePending = !await userManager.IsEmailConfirmedAsync(user),
            FirstName = claims.SingleOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value,
            LastName = claims.SingleOrDefault(x => x.Type == JwtClaimTypes.FamilyName)?.Value,
            OtherLogins = otherLogins,
            PhoneNumber = user.PhoneNumber,
            Tin = claims.SingleOrDefault(x => x.Type == BasicClaimTypes.Tin)?.Value,
            UserName = user.UserName,
            DeveloperTotp = claims.SingleOrDefault(x => x.Type == BasicClaimTypes.DeveloperTotp)?.Value,
            HasDeveloperTotp = configuration.DeveloperTotpEnabled() && roles.Contains(BasicRoleNames.Developer),
            Locale = claims.SingleOrDefault(x => x.Type == BasicClaimTypes.Locale)?.Value,
            ZoneInfo = claims.SingleOrDefault(x => x.Type == BasicClaimTypes.ZoneInfo)?.Value,
        });
    }
}
