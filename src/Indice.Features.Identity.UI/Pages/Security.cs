#if NET9_0_OR_GREATER
using Duende.IdentityModel;
#else
using IdentityModel;
#endif
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>
/// Page model for the security screen. It shows the profile picture, name and sign-in info of the user and the sign-in
/// methods (password, email, phone number and two-factor authentication) with links to the pages that change them.
/// </summary>
[Authorize]
[IdentityUI(typeof(SecurityModel))]
[SecurityHeaders]
public abstract class BaseSecurityModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseSecurityModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="identityUiOptions">Configuration options for Identity UI.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseSecurityModel(
        ExtendedUserManager<User> userManager,
        IOptions<IdentityUIOptions> identityUiOptions
    ) : base() {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        IdentityUIOptions = identityUiOptions?.Value ?? throw new ArgumentNullException(nameof(identityUiOptions));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>Configuration options for Identity UI.</summary>
    protected IdentityUIOptions IdentityUIOptions { get; }

    /// <summary>Security page view model.</summary>
    public SecurityViewModel View { get; set; } = new SecurityViewModel();

    /// <summary>Security page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        View = await BuildViewModelAsync();
        return Page();
    }

    /// <summary>Builds the security page view model.</summary>
    protected virtual async Task<SecurityViewModel> BuildViewModelAsync() {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        var claims = await UserManager.GetClaimsAsync(user);
        var givenName = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value;
        var familyName = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.FamilyName)?.Value;
        var viewModel = new SecurityViewModel {
            DisplayName = string.IsNullOrWhiteSpace(givenName) ? user.UserName : $"{givenName} {familyName}".Trim(),
            CreateDate = user.CreateDate,
            LastSignInDate = user.LastSignInDate,
            HasPassword = await UserManager.HasPasswordAsync(user),
            LastPasswordChangeDate = user.LastPasswordChangeDate,
            Email = user.Email,
            EmailConfirmed = !string.IsNullOrWhiteSpace(user.Email) && await UserManager.IsEmailConfirmedAsync(user),
            CanEditEmail = !IdentityUIOptions.DisableEmailEdit,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = !string.IsNullOrWhiteSpace(user.PhoneNumber) && await UserManager.IsPhoneNumberConfirmedAsync(user),
            CanEditPhone = !IdentityUIOptions.DisablePhoneEdit,
            TwoFactorEnabled = await UserManager.GetTwoFactorEnabledAsync(user)
        };
        var provider = ServiceProvider.GetService<IAuthenticationMethodProvider>();
        if (provider is null) {
            return viewModel;
        }
        var methods = await ManageMfaMethods.GetManageableMethodsAsync(provider);
        viewModel.CanManageMfa = !IdentityUIOptions.DisableMfaEdit && methods.Length > 0;
        if (viewModel.TwoFactorEnabled) {
            var method = await ManageMfaMethods.FindCurrentMethodAsync(provider, UserManager, user, methods) ?? await provider.FindMethodForUserOrDefaultAsync(user);
            viewModel.TwoFactorMethodName = method?.DisplayName;
        }
        return viewModel;
    }
}

internal class SecurityModel : BaseSecurityModel
{
    public SecurityModel(
        ExtendedUserManager<User> userManager,
        IOptions<IdentityUIOptions> identityUiOptions
    ) : base(userManager, identityUiOptions) { }
}
