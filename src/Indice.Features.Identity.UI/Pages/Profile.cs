#if NET9_0_OR_GREATER
using Duende.IdentityModel;
using Duende.IdentityServer.EntityFramework.DbContexts;

#else
using IdentityModel;
#endif
using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Extensions;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Indice.Globalization;
using Indice.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the profile screen.</summary>
[Authorize]
[IdentityUI(typeof(ProfileModel))]
[SecurityHeaders]
public abstract class BaseProfileModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseProfileModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="signInManager">Provides the APIs for user sign in.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="identityUiOptions">Configuration options for Identity UI.</param>
    /// <param name="localizationOptions">The request localization options</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseProfileModel(
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        IConfiguration configuration,
        IOptions<IdentityUIOptions> identityUiOptions,
        IOptions<RequestLocalizationOptions> localizationOptions
    ) : base() {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        SignInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        LocalizationOptions = localizationOptions.Value;
        IdentityUIOptions = identityUiOptions?.Value ?? throw new ArgumentNullException(nameof(identityUiOptions));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>Provides the APIs for user sign in.</summary>
    protected ExtendedSignInManager<User> SignInManager { get; }
    /// <summary>Represents a set of key/value application configuration properties.</summary>
    protected IConfiguration Configuration { get; }

    /// <summary>
    /// Gets the localization options used to configure request localization settings.
    /// </summary>
    protected RequestLocalizationOptions LocalizationOptions { get; }

    /// <summary>Configuration options for Identity UI.</summary>
    protected IdentityUIOptions IdentityUIOptions { get; set; }

    /// <summary>Manage profile page view model.</summary>
    public ProfileViewModel View { get; set; } = new ProfileViewModel();

    /// <summary>The profile data of the user. Profile details are edited on <c>/ChangeProfileDetails</c> and preferences on <c>/ChangePreferences</c>.</summary>
    public ProfileInputModel Input { get; set; } = new ProfileInputModel();

    /// <summary>Request input model for the manage profile page.</summary>
    [BindProperty]
    public LoginLinkInputModel InputLoginLink { get; set; } = new LoginLinkInputModel();

    /// <summary>Profile page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        Input = View = await BuildProfileViewModelAsync();
        return Page();
    }

    /// <summary>Profile page remove external login POST handler.</summary>
    public virtual async Task<IActionResult> OnPostRemoveLoginAsync() {
        var user = await UserManager.GetUserAsync(User);
        if (user == null) {
            TempData.Put("Alert", AlertModel.Error($"Unable to load user with ID '{UserManager.GetUserId(User)}'."));
            return RedirectToPage("/Profile");
        }
        var result = await UserManager.RemoveLoginAsync(user, InputLoginLink.LoginProvider!, InputLoginLink.ProviderKey!);
        if (!result.Succeeded) {
            TempData.Put("Alert", AlertModel.Error(string.Join(", ", result.Errors.Select(x => x.Description))));
            return RedirectToPage("/Profile");
        }
        await SignInManager.RefreshSignInAsync(user);
        TempData.Put("Alert", AlertModel.Success("Profile image changed."));
        return RedirectToPage("/Profile");
    }

    /// <summary>Profile page remove external login POST handler.</summary>
    public virtual async Task<IActionResult> OnPostUploadPictureAsync(IFormFile file) {
        var user = await UserManager.GetUserAsync(User);
        if (user == null) {
            TempData.Put("Alert", AlertModel.Error($"Unable to load user with ID '{UserManager.GetUserId(User)}'."));
            return RedirectToPage("/Profile");
        }
        if (!(file?.Length > 0)) {
            TempData.Put("Alert", AlertModel.Error($"file cannot be empty."));
            return RedirectToPage("/Profile");
        }
        if (file.Length > UiOptions.PictureUploadSizeLimit) {
            TempData.Put("Alert", AlertModel.Error($"file cannot over {UiOptions.PictureUploadSizeLimit.ToFileSize()}."));
            return RedirectToPage("/Profile");
        }
        var result = await UserManager.SetUserPictureAsync(user, file!.OpenReadStream(), UiOptions.PictureMaxSideSize);
        if (!result.Succeeded) {
            TempData.Put("Alert", AlertModel.Error(string.Join(", ", result.Errors.Select(x => x.Description))));
            return RedirectToPage("/Profile");
        }
        var cacheStore = ServiceProvider.GetService<Microsoft.AspNetCore.OutputCaching.IOutputCacheStore>();
        if (cacheStore is not null) {
            await cacheStore.EvictByTagAsync($"Picture|sub:{user.Id}", default);
            await cacheStore.EvictByTagAsync($"Picture|userId:{user.Id}", default);
        }
        return RedirectToPage("/Profile");
    }


    /// <summary>link an external login GET handler.</summary>
    public IActionResult OnGetLinkLogin(string provider) {
        var redirectUrl = Url.PageLink("/Profile", pageHandler: "LinkLoginCallback");
        var properties = SignInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, User.FindSubjectId());
        return new ChallengeResult(provider, properties);
    }

    /// <summary>link an external login callback GET handler.</summary>
    [HttpGet("link-login-callback")]
    public async Task<IActionResult> OnGetLinkLoginCallbackAsync() {
        var user = await UserManager.GetUserAsync(User);
        if (user == null) {
            TempData.Put("AlertProviders", AlertModel.Error($"Unable to load user with ID '{UserManager.GetUserId(User)}'."));
            return RedirectToPage("/Profile");
        }
        var userId = await UserManager.GetUserIdAsync(user);
        var externalLoginInfo = await SignInManager.GetExternalLoginInfoAsync(userId);
        if (externalLoginInfo is null) {
            return RedirectToPage("/Profile");
        }
        var result = await UserManager.AddLoginAsync(user, new UserLoginInfo(externalLoginInfo.LoginProvider, externalLoginInfo.ProviderKey, externalLoginInfo.LoginProvider));
        if (!result.Succeeded) {
            TempData.Put("AlertProviders", AlertModel.Error(string.Join(", ", result.Errors.Select(x => x.Description))));
            return RedirectToPage("/Profile");
        }
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        TempData.Put("AlertProviders", AlertModel.Success(UserManager.MessageDescriber.ProfileExternalLoginAddedSuccessMessage));
        return RedirectToPage("/Profile");
    }

    private async Task<ProfileViewModel> BuildProfileViewModelAsync() {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        var roles = await UserManager.GetRolesAsync(user);
        var claims = await UserManager.GetClaimsAsync(user);
        var birthDate = default(DateTime?);
        var birthDateText = claims.SingleOrDefault(x => x.Type == JwtClaimTypes.BirthDate)?.Value;
        if (birthDateText != null && DateTime.TryParse(birthDateText, out var date)) {
            birthDate = date;
        }

        var currentLogins = await UserManager.GetLoginsAsync(user);
        var otherLogins = (await SignInManager.GetExternalAuthenticationSchemesAsync())
            .Where(scheme => currentLogins.All(loginInfo => scheme.Name != loginInfo.LoginProvider))
            .ToList();
        var consentDateText = claims.SingleOrDefault(x => x.Type == BasicClaimTypes.ConsentCommercialDate)?.Value;
        var consentDate = new DateTime?();
        if (consentDateText != null && DateTime.TryParse(consentDateText, out date)) {
            consentDate = date;
        }
        _ = PhoneNumber.TryParse(user.PhoneNumber!, out var phoneNumber);

        var configurationDb = ServiceProvider.GetRequiredService<ExtendedConfigurationDbContext>();
        var userEditableClaimTypes = await ProfileClaims.GetUserEditableClaimTypesAsync(configurationDb);
        var canEditTin = userEditableClaimTypes.Any(x => x.Name == BasicClaimTypes.Tin);
        var canEditFamilyName = userEditableClaimTypes.Any(x => x.Name == BasicClaimTypes.FamilyName);
        var canEditGivenName = userEditableClaimTypes.Any(x => x.Name == BasicClaimTypes.GivenName);
        var viewModel = new ProfileViewModel {
            BirthDate = birthDate,
            CanRemoveProvider = await UserManager.HasPasswordAsync(user) || currentLogins.Count > 1,
            Claims = ProfileClaims.GetAdditionalClaims(userEditableClaimTypes, claims),
            DisableEditTin = !canEditTin,
            DisableEditFamilyName = !canEditFamilyName,
            DisableEditGivenName = !canEditGivenName,
            ConsentCommercial = claims.SingleOrDefault(x => x.Type == BasicClaimTypes.ConsentCommercial)?.Value == bool.TrueString.ToLower(),
            ConsentCommercialDate = consentDate,
            CurrentLogins = currentLogins,
            DeveloperTotp = claims.SingleOrDefault(x => x.Type == BasicClaimTypes.DeveloperTotp)?.Value,
            Email = user.Email ?? string.Empty,
            EmailChangePending = !await UserManager.IsEmailConfirmedAsync(user),
            FirstName = claims.SingleOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value,
            HasDeveloperTotp = Configuration.DeveloperTotpEnabled() && roles.Contains(BasicRoleNames.Developer),
            LastName = claims.SingleOrDefault(x => x.Type == JwtClaimTypes.FamilyName)?.Value,
            OtherLogins = otherLogins,
            SupportedCultures = LocalizationOptions.SupportedCultures ?? [],
            PhoneNumber = phoneNumber.Number,
            Tin = claims.SingleOrDefault(x => x.Type == BasicClaimTypes.Tin)?.Value,
            UserName = user.UserName ?? string.Empty,
            ZoneInfo = claims.SingleOrDefault(x => x.Type == JwtClaimTypes.ZoneInfo)?.Value,
            Locale = claims.SingleOrDefault(x => x.Type == JwtClaimTypes.Locale)?.Value,
            CallingCode = phoneNumber.CallingCode,
            CreateDate = user.CreateDate,
            LastSignInDate = user.LastSignInDate
        };
        return viewModel;
    }
}

internal class ProfileModel : BaseProfileModel
{
    public ProfileModel(
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        IConfiguration configuration,
        IOptions<IdentityUIOptions> identityUiOptions,
        IOptions<RequestLocalizationOptions> localizationOptions
    ) : base(userManager, signInManager, configuration, identityUiOptions, localizationOptions) { }
}
