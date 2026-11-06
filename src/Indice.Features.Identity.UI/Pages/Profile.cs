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

    /// <summary>Request input model for the manage profile page.</summary>
    [BindProperty]
    public ProfileInputModel Input { get; set; } = new ProfileInputModel();

    /// <summary>Request input model for the manage profile page.</summary>
    [BindProperty]
    public LoginLinkInputModel InputLoginLink { get; set; } = new LoginLinkInputModel();

    /// <summary>Request input model for the manage profile page.</summary>
    [BindProperty]
    public ProfileLanguagePreferenceInputModel InputLanguagePreference { get; set; } = new ProfileLanguagePreferenceInputModel();
    /// <summary></summary>
    [ViewData]
    public bool ProfileSuccessfullyChanged { get; set; }

    /// <summary></summary>
    [ViewData]
    public bool EmailChangeRequested { get; set; }

    /// <summary>Profile page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        Input = View = await BuildProfileViewModelAsync();
        return Page();
    }

    /// <summary>Profile page POST handler.</summary>
    public virtual async Task<IActionResult> OnPostAsync() {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        // Account fields (user name, email, phone) are updated through their dedicated handlers.
        foreach (var key in AccountFieldKeys.SelectMany(x => x.Value)) {
            ModelState.Remove(key);
        }
        _ = PhoneNumber.TryParse(user.PhoneNumber!, out var currentPhoneNumber);
        Input.UserName = user.UserName ?? string.Empty;
        Input.Email = user.Email ?? string.Empty;
        Input.PhoneNumber = currentPhoneNumber.Number;
        Input.CallingCode = currentPhoneNumber.CallingCode;
        View = await BuildProfileViewModelAsync(Input);
        if (!ModelState.IsValid) {
            return Page();
        }

        IdentityResult result;
        if (!View.DisableEditGivenName) {
            result = await UserManager.ReplaceClaimAsync(user, JwtClaimTypes.GivenName, Input.FirstName ?? string.Empty);
            AddModelErrors(result);
        }
        if (!View.DisableEditFamilyName) {
            result = await UserManager.ReplaceClaimAsync(user, JwtClaimTypes.FamilyName, Input.LastName ?? string.Empty);
            AddModelErrors(result);
        }
        if (!View.DisableEditTin) {
            result = await UserManager.ReplaceClaimAsync(user, BasicClaimTypes.Tin, Input.Tin ?? string.Empty);
            AddModelErrors(result);
        }
        result = await UserManager.ReplaceClaimAsync(user, JwtClaimTypes.BirthDate, Input.BirthDate.HasValue ? $"{Input.BirthDate:yyyy-MM-dd}" : string.Empty);
        AddModelErrors(result);
        result = await UserManager.ReplaceClaimAsync(user, BasicClaimTypes.ConsentCommercial, Input.ConsentCommercial ? bool.TrueString.ToLower() : bool.FalseString.ToLower());
        AddModelErrors(result);
        result = await UserManager.ReplaceClaimAsync(user, BasicClaimTypes.ConsentCommercialDate, $"{DateTime.UtcNow:O}");
        AddModelErrors(result);
        if (Input.ZoneInfo is not null && Input.ZoneInfo != user.Claims.FirstOrDefault(x => x.ClaimType == JwtClaimTypes.ZoneInfo)?.ClaimValue) {
            result = await UserManager.ReplaceClaimAsync(user, JwtClaimTypes.ZoneInfo, Input.ZoneInfo);
            AddModelErrors(result);
        }
        ProfileSuccessfullyChanged = ModelState.ErrorCount == 0;
        return Page();
    }

    /// <summary>Profile page update email POST handler. Starts the email change verification flow.</summary>
    public virtual async Task<IActionResult> OnPostUpdateEmailAsync() {
        if (IdentityUIOptions.DisableEmailEdit) {
            return BadRequest();
        }
        KeepModelStateFor(AccountFieldEmail);
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        if (!ModelState.IsValid) {
            return await AccountFieldPageAsync(AccountFieldEmail);
        }
        if (user.NormalizedEmail != Input.Email?.Trim().ToUpper() && !string.IsNullOrWhiteSpace(Input.Email)) {
            if (!await SendChangeEmailConfirmationEmail(user, Input.Email)) {
                ModelState.AddModelError(string.Empty, UserManager.MessageDescriber.LimitAttemptsReached);
                return await AccountFieldPageAsync(AccountFieldEmail);
            }
            EmailChangeRequested = true;
        }
        return await AccountFieldSuccessPageAsync();
    }

    /// <summary>Profile page update phone number POST handler.</summary>
    public virtual async Task<IActionResult> OnPostUpdatePhoneAsync() {
        if (IdentityUIOptions.DisablePhoneEdit) {
            return BadRequest();
        }
        KeepModelStateFor(AccountFieldPhone);
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        if (!ModelState.IsValid) {
            return await AccountFieldPageAsync(AccountFieldPhone);
        }
        // TODO: Replace direct update with the phone number verification flow.
        _ = PhoneNumber.TryParse(Input.PhoneNumberWithCallingCode!, out var phoneNumber);
        user.PhoneNumber = phoneNumber;
        var result = await UserManager.UpdateAsync(user);
        AddModelErrors(result);
        if (!result.Succeeded) {
            return await AccountFieldPageAsync(AccountFieldPhone);
        }
        await SignInManager.RefreshSignInAsync(user);
        return await AccountFieldSuccessPageAsync();
    }

    /// <summary>Profile page update user name POST handler.</summary>
    public virtual async Task<IActionResult> OnPostUpdateUserNameAsync() {
        if (IdentityUIOptions.DisableEmailEdit || UserManager.EmailAsUserName) {
            return BadRequest();
        }
        KeepModelStateFor(AccountFieldUserName);
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        if (!ModelState.IsValid) {
            return await AccountFieldPageAsync(AccountFieldUserName);
        }
        if (user.UserName != Input.UserName) {
            var result = await UserManager.SetUserNameAsync(user, Input.UserName);
            AddModelErrors(result);
            if (!result.Succeeded) {
                return await AccountFieldPageAsync(AccountFieldUserName);
            }
            await SignInManager.RefreshSignInAsync(user);
        }
        return await AccountFieldSuccessPageAsync();
    }

    /// <summary>Account field identifier for email.</summary>
    protected const string AccountFieldEmail = "Email";
    /// <summary>Account field identifier for phone number.</summary>
    protected const string AccountFieldPhone = "Phone";
    /// <summary>Account field identifier for user name.</summary>
    protected const string AccountFieldUserName = "UserName";

    private static readonly Dictionary<string, string[]> AccountFieldKeys = new() {
        [AccountFieldEmail] = [$"{nameof(Input)}.{nameof(ProfileInputModel.Email)}"],
        [AccountFieldPhone] = [
            $"{nameof(Input)}.{nameof(ProfileInputModel.CallingCode)}",
            $"{nameof(Input)}.{nameof(ProfileInputModel.PhoneNumber)}",
            $"{nameof(Input)}.{nameof(ProfileInputModel.PhoneNumberWithCallingCode)}"
        ],
        [AccountFieldUserName] = [$"{nameof(Input)}.{nameof(ProfileInputModel.UserName)}"]
    };

    private void KeepModelStateFor(string field) {
        var keep = AccountFieldKeys[field];
        foreach (var key in ModelState.Keys.Where(x => x != string.Empty && !keep.Contains(x)).ToList()) {
            ModelState.Remove(key);
        }
    }

    private async Task<IActionResult> AccountFieldPageAsync(string field) {
        var view = await BuildProfileViewModelAsync();
        view.EditingField = field;
        Input = View = view;
        return Page();
    }

    private async Task<IActionResult> AccountFieldSuccessPageAsync() {
        ModelState.Clear();
        var view = await BuildProfileViewModelAsync();
        view.EmailChangePending |= EmailChangeRequested;
        Input = View = view;
        ProfileSuccessfullyChanged = true;
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
    public virtual async Task<IActionResult> OnPostUpdateLanguagePreferenceAsync() {
        var user = await UserManager.GetUserAsync(User);
        if (user == null) {
            TempData.Put("Alert", AlertModel.Error($"Unable to load user with ID '{UserManager.GetUserId(User)}'."));
            return RedirectToPage("/Profile");
        }
        var result = await UserManager.ReplaceClaimAsync(user, JwtClaimTypes.Locale, InputLanguagePreference.Locale ?? string.Empty);
        if (!result.Succeeded) {
            TempData.Put("Alert", AlertModel.Error(string.Join(", ", result.Errors.Select(x => x.Description))));
            return RedirectToPage("/Profile");
        }
        await SignInManager.RefreshSignInAsync(user);
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
        var profileEditableClaimsList = await configurationDb.ClaimTypes.Where(x => x.Name == BasicClaimTypes.FamilyName || x.Name == BasicClaimTypes.GivenName || x.Name == BasicClaimTypes.Tin).ToListAsync();
        var canEditTin = profileEditableClaimsList.FirstOrDefault(x => x.Name == BasicClaimTypes.Tin)?.UserEditable ?? false;
        var canEditFamilyName = profileEditableClaimsList.FirstOrDefault(x => x.Name == BasicClaimTypes.FamilyName)?.UserEditable ?? false;
        var canEditGivenName = profileEditableClaimsList.FirstOrDefault(x => x.Name == BasicClaimTypes.GivenName)?.UserEditable ?? false;
        return new ProfileViewModel {
            BirthDate = birthDate,
            CanRemoveProvider = await UserManager.HasPasswordAsync(user) || currentLogins.Count > 1,
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
    }

    private async Task<ProfileViewModel> BuildProfileViewModelAsync(ProfileInputModel model) {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        var roles = await UserManager.GetRolesAsync(user);
        var currentLogins = await UserManager.GetLoginsAsync(user);
        var otherLogins = (await SignInManager.GetExternalAuthenticationSchemesAsync())
            .Where(scheme => currentLogins.All(loginInfo => scheme.Name != loginInfo.LoginProvider))
            .ToList();
        var configurationDb = ServiceProvider.GetRequiredService<ExtendedConfigurationDbContext>();
        var porfileEditableClaimsList = await configurationDb.ClaimTypes.Where(x => x.Name == BasicClaimTypes.FamilyName || x.Name == BasicClaimTypes.GivenName || x.Name == BasicClaimTypes.Tin).ToListAsync();
        var canEditTin = porfileEditableClaimsList.FirstOrDefault(x => x.Name == BasicClaimTypes.Tin)?.UserEditable ?? false;
        var canEditFamilyName = porfileEditableClaimsList.FirstOrDefault(x => x.Name == BasicClaimTypes.FamilyName)?.UserEditable ?? false;
        var canEditGivenName = porfileEditableClaimsList.FirstOrDefault(x => x.Name == BasicClaimTypes.GivenName)?.UserEditable ?? false;
        return new ProfileViewModel {
            BirthDate = model.BirthDate,
            CanRemoveProvider = await UserManager.HasPasswordAsync(user) || currentLogins.Count > 1,
            DisableEditTin = !canEditTin,
            DisableEditFamilyName = !canEditFamilyName,
            DisableEditGivenName = !canEditGivenName,
            ConsentCommercial = model.ConsentCommercial,
            ConsentCommercialDate = model.ConsentCommercialDate,
            CurrentLogins = currentLogins,
            DeveloperTotp = model.DeveloperTotp,
            Email = model.Email,
            EmailChangePending = !await UserManager.IsEmailConfirmedAsync(user) || EmailChangeRequested,
            FirstName = model.FirstName,
            HasDeveloperTotp = Configuration.DeveloperTotpEnabled() && roles.Contains(BasicRoleNames.Developer),
            LastName = model.LastName,
            OtherLogins = otherLogins,
            SupportedCultures = LocalizationOptions.SupportedCultures ?? [],
            PhoneNumber = IdentityUIOptions.EnablePhoneNumberCallingCodes ? model.PhoneNumberWithCallingCode : model.PhoneNumber,
            Tin = model.Tin,
            UserName = model.UserName,
            ZoneInfo = model.ZoneInfo ?? user.Claims.SingleOrDefault(x => x.ClaimType == JwtClaimTypes.ZoneInfo)?.ClaimValue,
            Locale = model.Locale ?? user.Claims.SingleOrDefault(x => x.ClaimType == JwtClaimTypes.Locale)?.ClaimValue,
            CallingCode = model.CallingCode,
            CreateDate = user.CreateDate,
            LastSignInDate = user.LastSignInDate
        };
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
