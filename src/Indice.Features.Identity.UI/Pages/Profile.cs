using System.Net.Http;
using IdentityModel;
using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Extensions;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Indice.Globalization;
using Indice.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
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
    /// <param name="localizer">The source of translations for this model class</param>
    /// <param name="identityUiOptions">Configuration options for Identity UI.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseProfileModel(
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        IConfiguration configuration,
        IStringLocalizer localizer,
        IOptions<IdentityUIOptions> identityUiOptions
    ) : base() {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        SignInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        IdentityUIOptions = identityUiOptions?.Value ?? throw new ArgumentNullException(nameof(identityUiOptions));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>Provides the APIs for user sign in.</summary>
    protected ExtendedSignInManager<User> SignInManager { get; }
    /// <summary>Represents a set of key/value application configuration properties.</summary>
    protected IConfiguration Configuration { get; }
    /// <summary>The source of translations for this model class.</summary>
    protected IStringLocalizer Localizer { get; }
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
        if (!ModelState.IsValid) {
            View = await BuildProfileViewModelAsync(Input);
            return Page();
        }
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        var result = await UserManager.ReplaceClaimAsync(user, JwtClaimTypes.GivenName, Input.FirstName!);
        AddModelErrors(result);
        result = await UserManager.ReplaceClaimAsync(user, JwtClaimTypes.FamilyName, Input.LastName!);
        AddModelErrors(result);
        result = await UserManager.ReplaceClaimAsync(user, BasicClaimTypes.Tin, Input.Tin ?? string.Empty);
        AddModelErrors(result);
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
        if (user.NormalizedEmail != Input.Email?.Trim().ToUpper()) {
            EmailChangeRequested = true;
            user.EmailConfirmed = false;
            await UserManager.SetEmailAsync(user, Input.Email);
            if (!string.IsNullOrWhiteSpace(Input.Email)) {
                await SendChangeEmailConfirmationEmail(user, Input.Email);
            }
        }
        if (!UserManager.EmailAsUserName && user.UserName != Input.UserName) {
            result = await UserManager.SetUserNameAsync(user, Input.UserName);
            AddModelErrors(result);
        }
        _ = PhoneNumber.TryParse(Input!.PhoneNumberWithCallingCode!, out var phoneNumber);
        user.PhoneNumber = IdentityUIOptions.EnablePhoneNumberCallingCodes ? phoneNumber : phoneNumber.Number;
        result = await UserManager.UpdateAsync(user);
        AddModelErrors(result);
        ProfileSuccessfullyChanged = ModelState.ErrorCount == 0;
        View = await BuildProfileViewModelAsync(Input);
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
        if (file?.Length > UiOptions.PictureUploadSizeLimit) {
            TempData.Put("Alert", AlertModel.Error($"file cannot over {UiOptions.PictureUploadSizeLimit.ToFileSize()}."));
            return RedirectToPage("/Profile");
        }
        var result = await UserManager.SetUserPictureAsync(user, file!.OpenReadStream(), UiOptions.PictureMaxSideSize);
        if (!result.Succeeded) {
            TempData.Put("Alert", AlertModel.Error(string.Join(", ", result.Errors.Select(x => x.Description))));
            return RedirectToPage("/Profile");
        }
#if NET7_0_OR_GREATER
        var cacheStore = ServiceProvider.GetService<Microsoft.AspNetCore.OutputCaching.IOutputCacheStore>();
        if (cacheStore is not null) {
            await cacheStore.EvictByTagAsync($"Picture|sub:{user.Id}", default);
            await cacheStore.EvictByTagAsync($"Picture|userId:{user.Id}", default);
        }
#endif    
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
        await HttpContext.SignOutAsync(SignInManager.ExternalScheme);
        TempData.Put("AlertProviders", AlertModel.Success(Localizer["The external login was added."]));
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
        return new ProfileViewModel {
            BirthDate = birthDate,
            CanRemoveProvider = await UserManager.HasPasswordAsync(user) || currentLogins.Count > 1,
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
            PhoneNumber = phoneNumber.Number,
            Tin = claims.SingleOrDefault(x => x.Type == BasicClaimTypes.Tin)?.Value,
            UserName = user.UserName ?? string.Empty,
            ZoneInfo = claims.SingleOrDefault(x => x.Type == JwtClaimTypes.ZoneInfo)?.Value,
            CallingCode = phoneNumber.CallingCode
        };
    }

    private async Task<ProfileViewModel> BuildProfileViewModelAsync(ProfileInputModel model) {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        var roles = await UserManager.GetRolesAsync(user);
        var currentLogins = await UserManager.GetLoginsAsync(user);
        var otherLogins = (await SignInManager.GetExternalAuthenticationSchemesAsync())
            .Where(scheme => currentLogins.All(loginInfo => scheme.Name != loginInfo.LoginProvider))
            .ToList();
        return new ProfileViewModel {
            BirthDate = model.BirthDate,
            CanRemoveProvider = await UserManager.HasPasswordAsync(user) || currentLogins.Count > 1,
            ConsentCommercial = model.ConsentCommercial,
            ConsentCommercialDate = model.ConsentCommercialDate,
            CurrentLogins = currentLogins,
            DeveloperTotp = model.DeveloperTotp,
            Email = model.Email,
            EmailChangePending = !await UserManager.IsEmailConfirmedAsync(user),
            FirstName = model.FirstName,
            HasDeveloperTotp = Configuration.DeveloperTotpEnabled() && roles.Contains(BasicRoleNames.Developer),
            LastName = model.LastName,
            OtherLogins = otherLogins,
            PhoneNumber = IdentityUIOptions.EnablePhoneNumberCallingCodes ? model.PhoneNumberWithCallingCode : model.PhoneNumber,
            Tin = model.Tin,
            UserName = model.UserName
        };
    }
}

internal class ProfileModel : BaseProfileModel
{
    public ProfileModel(
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        IConfiguration configuration,
        IStringLocalizer<ProfileModel> localizer,
        IOptions<IdentityUIOptions> identityUiOptions
    ) : base(userManager, signInManager, configuration, localizer, identityUiOptions) { }
}
