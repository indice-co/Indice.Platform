using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the external login screen.</summary>
[SecurityHeaders]
public abstract class BaseChallengeModel : BasePageModel
{
    private readonly IIdentityServerInteractionService _interaction;
    private readonly ExtendedSignInManager<User> _signInManager;
    private readonly ExtendedUserManager<User> _userManager;
    private readonly IEventService _events;

    /// <summary>Creates a new instance of <see cref="BaseChallengeModel"/> class.</summary>
    /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
    /// <param name="signInManager">Provides the APIs for user sign in.</param>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="events">Interface for the event service.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseChallengeModel(
        IIdentityServerInteractionService interaction,
        ExtendedSignInManager<User> signInManager,
        ExtendedUserManager<User> userManager,
        IEventService events
    ) : base() {
        _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    /// <summary>Challenge page GET handler.</summary>
    public IActionResult OnGet(string provider, string returnUrl, string prompt) {
        if (string.IsNullOrEmpty(returnUrl)) {
            returnUrl = "/";
        }
        if (Url.IsLocalUrl(returnUrl) == false && _interaction.IsValidReturnUrl(returnUrl) == false) {
            throw new Exception("Invalid return URL.");
        }
        var authenticationProperties = _signInManager.ConfigureExternalAuthenticationProperties(provider, Url.PageLink("Challenge", "Callback", new { returnUrl }));
        authenticationProperties.Items.Add(nameof(returnUrl), returnUrl);
        if (!string.IsNullOrWhiteSpace(prompt) && (prompt.Equals(OidcConstants.PromptModes.Login) || prompt.Equals(OidcConstants.PromptModes.SelectAccount))) {
            authenticationProperties.Items.Add(OidcConstants.AuthorizeRequest.Prompt, prompt);
        }
        return Challenge(authenticationProperties, provider);
    }

    /// <summary>Challenge callback page GET handler.</summary>
    public async Task<IActionResult> OnGetCallbackAsync(string returnUrl) {
        if (string.IsNullOrEmpty(returnUrl)) {
            returnUrl = "/";
        };
        if (!Url.IsLocalUrl(returnUrl) && !_interaction.IsValidReturnUrl(returnUrl)) {
            throw new Exception("Invalid return URL.");
        }
        var externalLoginInfo = await _signInManager.GetExternalLoginInfoAsync() ?? throw new Exception($"Cannot read external login information from external provider.");
        var user = await _userManager.FindByLoginAsync(externalLoginInfo.LoginProvider, externalLoginInfo.ProviderKey);
        if (user is null) {
            var claims = externalLoginInfo.Principal.Claims.ToList();
            TempData.Put("UserDetails", new AssociateViewModel {
                UserName = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Email)?.Value,
                Email = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Email)?.Value,
                FirstName = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value,
                LastName = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.FamilyName)?.Value,
                PhoneNumber = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.PhoneNumber)?.Value,
                Provider = externalLoginInfo.LoginProvider,
                ReturnUrl = returnUrl
            });
            return RedirectToPage("Associate");
        }
        await _events.RaiseAsync(new UserLoginSuccessEvent(externalLoginInfo.LoginProvider, externalLoginInfo.Principal.GetSubjectId(), user.Id, user.UserName));
        // Save user tokes retrieved from external provider.
        await _signInManager.UpdateExternalAuthenticationTokensAsync(externalLoginInfo);
        var result = await _signInManager.ExternalLoginSignInAsync(externalLoginInfo.LoginProvider, externalLoginInfo.ProviderKey, isPersistent: true);
        var redirectResult = GetRedirectToPageResult(result, returnUrl);
        if (redirectResult is not null) {
            return redirectResult;
        }
        // Check if external login is in the context of an OIDC request.
        var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
        if (context is not null) {
            if (context.IsNativeClient()) {
                // The client is native, so this change in how to return the response is for better UX for the end user.
                return this.LoadingPage("Redirect", returnUrl);
            }
        }
        return Redirect(returnUrl);
    }
}

internal class ChallengeModel : BaseChallengeModel
{
    public ChallengeModel(
        IIdentityServerInteractionService interaction,
        ExtendedSignInManager<User> signInManager,
        ExtendedUserManager<User> userManager,
        IEventService events
    ) : base(interaction, signInManager, userManager, events) { }
}
