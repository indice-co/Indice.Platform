using System.Security.Claims;
using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Identity.Controllers;

/// <summary>Contains methods that help a user to login using external providers.</summary>
[ApiExplorerSettings(IgnoreApi = true)]
[SecurityHeaders]
public class ExternalController : Controller
{
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IClientStore _clientStore;
    private readonly IEventService _events;
    private readonly ExtendedSignInManager<User> _signInManager;
    private readonly ExtendedUserManager<User> _userManager;
    /// <summary>The name of the controller.</summary>
    public const string Name = "External";

    public ExternalController(
        IIdentityServerInteractionService interaction,
        IClientStore clientStore,
        IEventService events,
        ExtendedSignInManager<User> signInManager,
        ExtendedUserManager<User> userManager
    ) {
        _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
        _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    [HttpGet]
    public IActionResult Challenge(string provider, string returnUrl) {
        if (string.IsNullOrEmpty(returnUrl)) {
            returnUrl = "~/";
        }
        // Validate returnUrl - either it is a valid OIDC URL or back to a local page.
        if (Url.IsLocalUrl(returnUrl) == false && _interaction.IsValidReturnUrl(returnUrl) == false) {
            // User might have clicked on a malicious link - should be logged.
            throw new Exception("Invalid return URL.");
        }
        var authenticationProperties = _signInManager.ConfigureExternalAuthenticationProperties(provider, Url.Action(nameof(Callback), new { returnUrl }));
        authenticationProperties.Items.Add(nameof(returnUrl), returnUrl);
        return Challenge(authenticationProperties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> Callback(string returnUrl) {
        if (string.IsNullOrEmpty(returnUrl)) {
            returnUrl = "~/";
        };
        // Validate returnUrl - either it is a valid OIDC URL or back to a local page.
        if (Url.IsLocalUrl(returnUrl) == false && _interaction.IsValidReturnUrl(returnUrl) == false) {
            // User might have clicked on a malicious link - should be logged.
            throw new Exception("Invalid return URL.");
        }
        // Read external identity from the temporary cookie.
        // At this point registered claims transformation will run (the ClaimsTransformer class).
        // This is triggered when HttpContext.AuthenticateAsync method is called (which is done inside GetExternalLoginInfoAsync method of SignInManager).
        var externalLoginInfo = await _signInManager.GetExternalLoginInfoAsync();
        if (externalLoginInfo == null) {
            throw new Exception($"Cannot read external login information from external login provider.");
        }
        var user = await _userManager.FindByLoginAsync(externalLoginInfo.LoginProvider, externalLoginInfo.ProviderKey);
        // If user with the specified login is null, this means that we have to do with a brand new user.
        if (user == null) {
            // Retrieve claims of the external user.
            var claims = externalLoginInfo.Principal.Claims.ToList();
            // We can choose to auto-provision the user or initiate a custom workflow for user registration.
            var autoProvisionResult = await AutoProvisionExternalUser(Guid.NewGuid().ToString(), claims);
            if (autoProvisionResult.Succeeded) {
                user = autoProvisionResult.User;
            } else {
                var loginTempData = new LoginTempData { Errors = autoProvisionResult.Errors };
                TempData.Put(nameof(loginTempData), loginTempData);
                return RedirectToAction("Login", AccountController.Name);
            }
            // Save user external login.
            await _userManager.AddLoginAsync(user, externalLoginInfo);
        }
        await _events.RaiseAsync(new UserLoginSuccessEvent(externalLoginInfo.LoginProvider, externalLoginInfo.Principal.GetSubjectId(), user.Id, user.UserName));
        // Save user tokes retrieved from external provider.
        await _signInManager.UpdateExternalAuthenticationTokensAsync(externalLoginInfo);
        await _signInManager.ExternalLoginSignInAsync(externalLoginInfo.LoginProvider, externalLoginInfo.ProviderKey, isPersistent: true);
        // Check if external login is in the context of an OIDC request.
        var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
        if (context != null) {
            /*if (context.IsNativeClient()) {
                // The client is native, so this change in how to
                // return the response is for better UX for the end user.
                return this.LoadingPage("Redirect", returnUrl);
            }*/
        }
        return Redirect(returnUrl);
    }

    private async Task<(User User, bool Succeeded, IEnumerable<string> Errors)> AutoProvisionExternalUser(string userId, List<Claim> claims) {
        var email = claims.Single(x => x.Type == JwtClaimTypes.Email).Value;
        // New user auto-registration flow.
        var user = new User(email, userId) {
            Email = email,
            EmailConfirmed = true
        };
        // Add claims from the external provider.
        foreach (var claim in claims) {
            if (claim.Type == JwtClaimTypes.Subject || claim.Type == JwtClaimTypes.Email) {
                continue;
            }
            user.Claims.Add(new IdentityUserClaim<string> {
                ClaimType = claim.Type,
                ClaimValue = claim.Value,
                UserId = userId
            });
        }
        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded) {
            return (null, false, result.Errors.Select(x => x.Description));
        }
        return (user, true, new List<string>());
    }
}
