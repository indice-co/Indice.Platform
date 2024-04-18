using System.Security.Claims;
using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the external login screen.</summary>
[IdentityUI(typeof(ChallengeModel))]
[SecurityHeaders]
public abstract class BaseChallengeModel : BasePageModel
{
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
        Interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
        SignInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        Events = events ?? throw new ArgumentNullException(nameof(events));
    }

    /// <summary>Provide services be used by the user interface to communicate with IdentityServer.</summary>
    protected IIdentityServerInteractionService Interaction { get; }
    /// <summary>Provides the APIs for user sign in.</summary>
    protected ExtendedSignInManager<User> SignInManager { get; }
    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>Interface for the event service.</summary>
    protected IEventService Events { get; }

    /// <summary>Challenge page GET handler.</summary>
    public IActionResult OnGet(string provider, string returnUrl, string prompt) {
        if (string.IsNullOrEmpty(returnUrl)) {
            returnUrl = "/";
        }
        if (Url.IsLocalUrl(returnUrl) == false && Interaction.IsValidReturnUrl(returnUrl) == false) {
            throw new Exception("Invalid return URL.");
        }
        var authenticationProperties = SignInManager.ConfigureExternalAuthenticationProperties(provider, Url.PageLink("/Challenge", "Callback", new { returnUrl }));
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
        if (!Url.IsLocalUrl(returnUrl) && !Interaction.IsValidReturnUrl(returnUrl)) {
            throw new Exception("Invalid return URL.");
        }
        var externalLoginInfo = await SignInManager.GetExternalLoginInfoAsync() ?? throw new Exception($"Cannot read external login information from external provider.");
        var user = await UserManager.FindByLoginAsync(externalLoginInfo.LoginProvider, externalLoginInfo.ProviderKey);
        if (user is null) {
            return await UserNotFound(externalLoginInfo, returnUrl);
        }
        await Events.RaiseAsync(new ExtendedUserLoginSuccessEvent(externalLoginInfo.LoginProvider, externalLoginInfo.Principal.GetSubjectId(), user.Id, user.UserName));
        // Save user tokes retrieved from external provider.
        await SignInManager.UpdateExternalAuthenticationTokensAsync(externalLoginInfo);
        var result = await SignInManager.ExternalLoginSignInAsync(externalLoginInfo.LoginProvider, externalLoginInfo.ProviderKey, isPersistent: true);

        await UserUpdateFromExternalInformation(user, externalLoginInfo);

        // Replace locale Claim only if it has a different value configured.
        var localeClaim = user.Claims.FirstOrDefault(x => x.ClaimType == JwtClaimTypes.Locale && x.ClaimValue == RequestCulture.Culture.TwoLetterISOLanguageName);
        if (localeClaim is null) {
            await UserManager.ReplaceClaimAsync(user, JwtClaimTypes.Locale, RequestCulture.Culture.TwoLetterISOLanguageName);
        }
        var redirectUrl = GetRedirectUrl(result, returnUrl);
        if (redirectUrl is not null) {
            return Redirect(redirectUrl);
        }
        // Check if external login is in the context of an OIDC request.
        var context = await Interaction.GetAuthorizationContextAsync(returnUrl);
        if (context is not null) {
            if (context.IsNativeClient()) {
                // The client is native, so this change in how to return the response is for better UX for the end user.
                return this.LoadingPage("Redirect", returnUrl);
            }
        }
        return Redirect(returnUrl);
    }

    /// <summary>This is called whenever a user is not found by an associated external identity provider.</summary>
    /// <param name="externalLoginInfo">Represents login information, source and externally source principal for a user record.</param>
    /// <param name="returnUrl">The return URL.</param>
    [NonAction]
    protected virtual async Task<IActionResult> UserNotFound(ExternalLoginInfo externalLoginInfo, string returnUrl) {
        await Task.CompletedTask;
        var claims = externalLoginInfo.Principal.Claims.ToList();
        TempData.Put("UserDetails", new AssociateViewModel {
            UserName = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Email)?.Value,
            Email = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Email)?.Value ?? string.Empty,
            FirstName = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value ?? string.Empty,
            LastName = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.FamilyName)?.Value ?? string.Empty,
            PhoneNumber = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.PhoneNumber)?.Value,
            Provider = externalLoginInfo.LoginProvider,
            ReturnUrl = returnUrl
        });
        return RedirectToPage("/Associate");
    }

    /// <summary>This is called whenever a user is found and its the last resort to update any Roles or claims based on whet the external provider offers.</summary>
    /// <param name="user">The <see cref="User"/> that was picked up in the system database using the external info.</param>
    /// <param name="externalLoginInfo">Represents login information, source and externally source principal for a user record.</param>
    /// <remarks>The base implementation is empty and serves only as an extensibility point</remarks>
    /// <returns>A Task</returns>
    [NonAction]
    protected virtual Task UserUpdateFromExternalInformation(User user, ExternalLoginInfo externalLoginInfo) {
        return Task.CompletedTask;
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
