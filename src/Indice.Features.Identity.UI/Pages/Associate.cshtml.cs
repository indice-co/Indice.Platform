using System.Security.Claims;
using System.Text;
using IdentityModel;
using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Indice.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the external login screen.</summary>
[SecurityHeaders]
public class AssociatePageModel : BasePageModel
{
    private readonly ExtendedSignInManager<User> _signInManager;
    private readonly ExtendedUserManager<User> _userManager;

    /// <summary>Creates a new instance of <see cref="LoginPageModel"/> class.</summary>
    /// <param name="signInManager">Provides the APIs for user sign in.</param>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AssociatePageModel(
        ExtendedSignInManager<User> signInManager,
        ExtendedUserManager<User> userManager,
        IServiceProvider serviceProvider
    ) : base(serviceProvider) {
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <summary>The view model that backs the external provider association page.</summary>
    public AssociateViewModel ViewModel { get; set; }

    /// <summary>The input model that backs the external provider association page.</summary>
    [BindProperty]
    public AssociateInputModel Input { get; set; }

    /// <summary>Associate page GET handler.</summary>
    public IActionResult OnGet() {
        // if i got here then there was an external login for a new user not present in the database.
        // This following view will help to review the data coming in before proceeding with the user provisioning.
        var associateViewModel = TempData.Peek<AssociateViewModel>("UserDetails");
        if (associateViewModel is null) {
            return RedirectToPage("Login");
        }
        Input = ViewModel = associateViewModel;
        return Page();
    }

    /// <summary>Associate page POST handler.</summary>
    public async Task<IActionResult> OnPostAsync(string returnUrl) {
        if (!ModelState.IsValid) {
            var associateViewModel = TempData.Peek<AssociateViewModel>("UserDetails");
            if (associateViewModel is null) {
                return RedirectToPage("Login");
            }
            return Page();
        }
        var externalLoginInfo = await _signInManager.GetExternalLoginInfoAsync();
        var claims = externalLoginInfo.Principal.Claims.ToList();
        claims.RemoveAll(x => x.Type == JwtClaimTypes.Subject);
        claims.RemoveAll(x => x.Type == JwtClaimTypes.GivenName);
        claims.RemoveAll(x => x.Type == JwtClaimTypes.FamilyName);
        claims.RemoveAll(x => x.Type == JwtClaimTypes.Name);
        claims.RemoveAll(x => x.Type == JwtClaimTypes.EmailVerified);
        claims.RemoveAll(x => x.Type == JwtClaimTypes.PhoneNumberVerified);
        claims.Add(new Claim(JwtClaimTypes.GivenName, Input.FirstName ?? string.Empty));
        claims.Add(new Claim(JwtClaimTypes.FamilyName, Input.LastName ?? string.Empty));
        claims.Add(new Claim(BasicClaimTypes.ConsentCommencial, Input.HasAcceptedTerms ? bool.TrueString.ToLower() : bool.FalseString.ToLower()));
        claims.Add(new Claim(BasicClaimTypes.ConsentTerms, Input.HasReadPrivacyPolicy ? bool.TrueString.ToLower() : bool.FalseString.ToLower()));
        claims.Add(new Claim(BasicClaimTypes.ConsentTermsDate, $"{DateTime.UtcNow:O}"));
        claims.Add(new Claim(BasicClaimTypes.ConsentCommencialDate, $"{DateTime.UtcNow:O}"));
        var user = await ProvisionExternalUser(Input.UserName, Input.PhoneNumber, claims);
        await _userManager.AddLoginAsync(user, new UserLoginInfo(externalLoginInfo.LoginProvider, externalLoginInfo.ProviderKey, externalLoginInfo.ProviderDisplayName ?? externalLoginInfo.LoginProvider));
        return RedirectToPage("Challenge", "Callback", routeValues: new { returnUrl = Input.ReturnUrl });
    }

    private async Task<User> ProvisionExternalUser(string userName, string phoneNumber, List<Claim> claims) {
        var emailClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Email);
        claims.Remove(emailClaim);
        var phoneClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.PhoneNumber);
        if (phoneClaim is not null) claims.Remove(phoneClaim);
        var givenNameClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName);
        var familyNameClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.FamilyName);
        var email = emailClaim.Value;
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null) {
            if (!user.EmailConfirmed) {
                await SendConfirmationEmail(user);
                throw new Exception("User exists as a local account but the email is not yet confirmed. If you are the owner please confirm your email first so that the accounts can be merged.");
            }
        } else {
            var userId = Guid.NewGuid().ToString();
            user = new User(userName, userId) {
                Email = email,
                UserName = userName,
                EmailConfirmed = userName == email,
                PhoneNumber = phoneNumber
            };
            foreach (var claim in claims) {
                user.Claims.Add(new IdentityUserClaim<string> {
                    ClaimType = claim.Type,
                    ClaimValue = claim.Value,
                    UserId = userId
                });
            }
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded) {
                var errors = new StringBuilder();
                foreach (var error in result.Errors) {
                    errors.AppendLine(error.Description);
                }
                throw new Exception($"Failed to provision automatically external user: {errors}.");
            }
            if (!user.EmailConfirmed) {
                await SendConfirmationEmail(user);
            }
        }
        return user;
    }
}
