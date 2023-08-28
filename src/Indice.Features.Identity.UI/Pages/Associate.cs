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
[IdentityUI(typeof(AssociateModel))]
[SecurityHeaders]
public abstract class BaseAssociateModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseLoginModel"/> class.</summary>
    /// <param name="signInManager">Provides the APIs for user sign in.</param>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseAssociateModel(
        ExtendedSignInManager<User> signInManager,
        ExtendedUserManager<User> userManager
    ) : base() {
        SignInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <summary>Provides the APIs for user sign in.</summary>
    protected ExtendedSignInManager<User> SignInManager { get; }
    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }

    /// <summary>The view model that backs the external provider association page.</summary>
    public AssociateViewModel View { get; set; } = new AssociateViewModel();

    /// <summary>The input model that backs the external provider association page.</summary>
    [BindProperty]
    public AssociateInputModel Input { get; set; } = new AssociateInputModel();

    /// <summary>Associate page GET handler.</summary>
    public async Task<IActionResult> OnGet() {
        // if i got here then there was an external login for a new user not present in the database.
        // This following view will help to review the data coming in before proceeding with the user provisioning.
        var associateViewModel = TempData.Peek<AssociateViewModel>("UserDetails");
        if (associateViewModel is null) {
            return RedirectToPage("/Login");
        }
        Input = View = associateViewModel;
        if (UiOptions.AutoProvisionExternalUsers) {
            return await OnPostAsync();
        }
        return Page();
    }

    /// <summary>Associate page POST handler.</summary>
    public virtual async Task<IActionResult> OnPostAsync() {
        if (!ModelState.IsValid) {
            return Page();
        }
        var externalLoginInfo = await SignInManager.GetExternalLoginInfoAsync();
        var claims = externalLoginInfo.Principal.Claims.ToList();
        claims.RemoveAll(x => x.Type == JwtClaimTypes.Subject);
        claims.RemoveAll(x => x.Type == JwtClaimTypes.GivenName);
        claims.RemoveAll(x => x.Type == JwtClaimTypes.FamilyName);
        claims.RemoveAll(x => x.Type == JwtClaimTypes.Name);
        claims.RemoveAll(x => x.Type == JwtClaimTypes.EmailVerified);
        claims.RemoveAll(x => x.Type == JwtClaimTypes.PhoneNumberVerified);
        claims.Add(new Claim(JwtClaimTypes.GivenName, Input.FirstName ?? string.Empty));
        claims.Add(new Claim(JwtClaimTypes.FamilyName, Input.LastName ?? string.Empty));
        claims.Add(new Claim(BasicClaimTypes.ConsentCommercial, Input.HasAcceptedTerms ? bool.TrueString.ToLower() : bool.FalseString.ToLower()));
        claims.Add(new Claim(BasicClaimTypes.ConsentTerms, Input.HasReadPrivacyPolicy ? bool.TrueString.ToLower() : bool.FalseString.ToLower()));
        claims.Add(new Claim(BasicClaimTypes.ConsentTermsDate, $"{DateTime.UtcNow:O}"));
        claims.Add(new Claim(BasicClaimTypes.ConsentCommercialDate, $"{DateTime.UtcNow:O}"));
        await AddExtraClaims(claims);
        var user = await FindOrCreateUser(Input.UserName, Input.PhoneNumber, claims);
        await UserManager.AddLoginAsync(user, new UserLoginInfo(externalLoginInfo.LoginProvider, externalLoginInfo.ProviderKey, externalLoginInfo.ProviderDisplayName ?? externalLoginInfo.LoginProvider));
        return RedirectToPage("/Challenge", "Callback", new {
            returnUrl = Input.ReturnUrl
        });
    }

    /// <summary>Will be called each time a new user is going to be provisioned. Can implement custom input model or Temp Data model form an OnBoarding process to drive the additional claims needed.</summary>
    /// <param name="userClaims">The collection of claims on the user.</param>
    public abstract Task AddExtraClaims(List<Claim> userClaims);

    /// <summary>Provision external user.</summary>
    /// <param name="userName">The username.</param>
    /// <param name="phoneNumber">The phone number.</param>
    /// <param name="claims">Additional claims.</param>
    /// <exception cref="Exception"></exception>
    [NonAction]
    protected async Task<User> FindOrCreateUser(string? userName, string? phoneNumber, List<Claim> claims) {
        var emailClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Email);
        if (emailClaim is not null) {
            claims.Remove(emailClaim);
        }
        var phoneClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.PhoneNumber);
        if (phoneClaim is not null) {
            claims.Remove(phoneClaim);
        }
        var givenNameClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName);
        var familyNameClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.FamilyName);
        var email = emailClaim?.Value;
        if (!string.IsNullOrWhiteSpace(email)) {
            // Try find existing user.
            var user = await UserManager.FindByEmailAsync(email);
            if (user is not null) {
                if (!user.EmailConfirmed) {
                    await SendConfirmationEmail(user);
                    throw new Exception("User exists as a local account but the email is not yet confirmed. If you are the owner please confirm your email first so that the accounts can be merged.");
                }
                return user;
            }
        }
        // New user flow.
        var userId = Guid.NewGuid().ToString();
        var newUser = new User(userName, userId) {
            Email = email,
            UserName = userName,
            EmailConfirmed = userName == email,
            PhoneNumber = phoneNumber
        };
        foreach (var claim in claims) {
            newUser.Claims.Add(new IdentityUserClaim<string> {
                ClaimType = claim.Type,
                ClaimValue = claim.Value,
                UserId = userId
            });
        }
        var result = await UserManager.CreateAsync(newUser);
        if (!result.Succeeded) {
            var errors = new StringBuilder();
            foreach (var error in result.Errors) {
                errors.AppendLine(error.Description);
            }
            throw new Exception($"Failed to provision automatically external user: {errors}.");
        }
        if (!newUser.EmailConfirmed) {
            await SendConfirmationEmail(newUser);
        }
        return newUser;
    }
}

internal class AssociateModel : BaseAssociateModel
{
    public AssociateModel(
        ExtendedSignInManager<User> signInManager,
        ExtendedUserManager<User> userManager
    ) : base(signInManager, userManager) { }

    public override Task AddExtraClaims(List<Claim> userClaims) => Task.CompletedTask;
}
