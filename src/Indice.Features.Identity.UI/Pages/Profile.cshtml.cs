using IdentityModel;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the profile screen.</summary>
[Authorize]
[IdentityUI(typeof(ProfileModel))]
[SecurityHeaders]
public abstract class BaseProfileModel : BasePageModel
{
    private readonly ExtendedUserManager<User> _userManager;
    private readonly ExtendedSignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;

    /// <summary>Creates a new instance of <see cref="BaseProfileModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="signInManager">Provides the APIs for user sign in.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseProfileModel(
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        IConfiguration configuration
    ) : base() {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>Manage profile page view model.</summary>
    public ProfileViewModel View { get; set; } = new ProfileViewModel();

    /// <summary>Request input model for the manage profile page.</summary>
    [BindProperty]
    public ProfileInputModel Input { get; set; } = new ProfileInputModel();

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
        var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        var result = await _userManager.ReplaceClaimAsync(user, JwtClaimTypes.GivenName, Input.FirstName);
        AddModelErrors(result);
        result = await _userManager.ReplaceClaimAsync(user, JwtClaimTypes.FamilyName, Input.LastName);
        AddModelErrors(result);
        result = await _userManager.ReplaceClaimAsync(user, BasicClaimTypes.Tin, Input.Tin ?? string.Empty);
        AddModelErrors(result);
        result = await _userManager.ReplaceClaimAsync(user, JwtClaimTypes.BirthDate, Input.BirthDate.HasValue ? $"{Input.BirthDate:yyyy-MM-dd}" : string.Empty);
        AddModelErrors(result);
        result = await _userManager.ReplaceClaimAsync(user, BasicClaimTypes.ConsentCommencial, Input.ConsentCommercial ? bool.TrueString.ToLower() : bool.FalseString.ToLower());
        AddModelErrors(result);
        result = await _userManager.ReplaceClaimAsync(user, BasicClaimTypes.ConsentCommencialDate, $"{DateTime.UtcNow:O}");
        AddModelErrors(result);
        if (user.NormalizedEmail != Input.Email?.Trim().ToUpper()) {
            EmailChangeRequested = true;
            user.EmailConfirmed = false;
            await _userManager.SetEmailAsync(user, Input.Email);
            if (!string.IsNullOrWhiteSpace(Input.Email)) {
                await SendChangeEmailConfirmationEmail(user, Input.Email);
            }
        }
        user.PhoneNumber = Input.PhoneNumber;
        if (user.UserName != Input.UserName) {
            result = await _userManager.SetUserNameAsync(user, Input.UserName);
            AddModelErrors(result);
        }
        result = await _userManager.UpdateAsync(user);
        AddModelErrors(result);
        ProfileSuccessfullyChanged = ModelState.ErrorCount == 0;
        View = await BuildProfileViewModelAsync(Input);
        return Page();
    }

    private async Task<ProfileViewModel> BuildProfileViewModelAsync() {
        var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);
        var birthDate = default(DateTime?);
        var birthDateText = claims.SingleOrDefault(x => x.Type == JwtClaimTypes.BirthDate)?.Value;
        if (birthDateText != null && DateTime.TryParse(birthDateText, out var date)) {
            birthDate = date;
        }
        var currentLogins = await _userManager.GetLoginsAsync(user);
        var otherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
            .Where(scheme => currentLogins.All(loginInfo => scheme.Name != loginInfo.LoginProvider))
            .ToList();
        var consentDateText = claims.SingleOrDefault(x => x.Type == BasicClaimTypes.ConsentCommencialDate)?.Value;
        var consentDate = new DateTime?();
        if (consentDateText != null && DateTime.TryParse(consentDateText, out date)) {
            consentDate = date;
        }
        return new ProfileViewModel {
            BirthDate = birthDate,
            CanRemoveProvider = await _userManager.HasPasswordAsync(user) || currentLogins.Count > 1,
            ConsentCommercial = claims.SingleOrDefault(x => x.Type == BasicClaimTypes.ConsentCommencial)?.Value == bool.TrueString.ToLower(),
            ConsentCommercialDate = consentDate,
            CurrentLogins = currentLogins,
            DeveloperTotp = claims.SingleOrDefault(x => x.Type == BasicClaimTypes.DeveloperTotp)?.Value,
            Email = user.Email ?? string.Empty,
            EmailChangePending = !await _userManager.IsEmailConfirmedAsync(user),
            FirstName = claims.SingleOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value,
            HasDeveloperTotp = _configuration.DeveloperTotpEnabled() && roles.Contains(BasicRoleNames.Developer),
            LastName = claims.SingleOrDefault(x => x.Type == JwtClaimTypes.FamilyName)?.Value,
            OtherLogins = otherLogins,
            PhoneNumber = user.PhoneNumber,
            Tin = claims.SingleOrDefault(x => x.Type == BasicClaimTypes.Tin)?.Value,
            UserName = user.UserName ?? string.Empty
        };
    }

    private async Task<ProfileViewModel> BuildProfileViewModelAsync(ProfileInputModel model) {
        var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        var roles = await _userManager.GetRolesAsync(user);
        var currentLogins = await _userManager.GetLoginsAsync(user);
        var otherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
            .Where(scheme => currentLogins.All(loginInfo => scheme.Name != loginInfo.LoginProvider))
            .ToList();
        return new ProfileViewModel {
            BirthDate = model.BirthDate,
            CanRemoveProvider = await _userManager.HasPasswordAsync(user) || currentLogins.Count > 1,
            ConsentCommercial = model.ConsentCommercial,
            ConsentCommercialDate = model.ConsentCommercialDate,
            CurrentLogins = currentLogins,
            DeveloperTotp = model.DeveloperTotp,
            Email = model.Email,
            EmailChangePending = !await _userManager.IsEmailConfirmedAsync(user),
            FirstName = model.FirstName,
            HasDeveloperTotp = _configuration.DeveloperTotpEnabled() && roles.Contains(BasicRoleNames.Developer),
            LastName = model.LastName,
            OtherLogins = otherLogins,
            PhoneNumber = model.PhoneNumber,
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
        IConfiguration configuration
    ) : base(userManager, signInManager, configuration) { }
}
