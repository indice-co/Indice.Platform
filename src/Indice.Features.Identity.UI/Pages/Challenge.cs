using System.Security.Claims;
#if NET9_0_OR_GREATER
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Services;
using Duende.IdentityModel;
#else
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityModel;
#endif
using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;


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
    /// <param name="schemeProvider">Provides access to authentication schemes.</param>
    /// <param name="logger">A generic interface for logging</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseChallengeModel(
        IIdentityServerInteractionService interaction,
        ExtendedSignInManager<User> signInManager,
        ExtendedUserManager<User> userManager,
        IEventService events,
        IAuthenticationSchemeProvider schemeProvider,
        ILogger<BaseChallengeModel> logger
    ) : base() {
        Interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
        SignInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        SchemeProvider = schemeProvider ?? throw new ArgumentNullException(nameof(schemeProvider));
    }

    /// <summary>Provide services be used by the user interface to communicate with IdentityServer.</summary>
    protected IIdentityServerInteractionService Interaction { get; }
    /// <summary>Provides the APIs for user sign in.</summary>
    protected ExtendedSignInManager<User> SignInManager { get; }
    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>Interface for the event service.</summary>
    protected IEventService Events { get; }
    /// <summary>Represents a type used to perform logging.</summary>
    protected ILogger<BaseChallengeModel> Logger;
    /// <summary>Responsible for managing what authentication schemes are supported.</summary>
    protected IAuthenticationSchemeProvider SchemeProvider { get; }

    /// <summary>Challenge page GET handler.</summary>
    public async Task<IActionResult> OnGet(string provider, string returnUrl, string prompt) {
        if (string.IsNullOrEmpty(returnUrl)) {
            returnUrl = "/";
        }
        if (string.IsNullOrEmpty(provider)) {
            Logger.LogError("No external provider specified for authentication.");
            return await RedirectToErrorPageAsync(HttpContext, "No provider", "No external provider specified for authentication.");
        }
        var schemes = await SchemeProvider.GetAllSchemesAsync();
        var providers = schemes
           .Where(x => x.DisplayName is not null)
           .Select(x => new ExternalProviderModel {
               DisplayName = x.DisplayName ?? x.Name,
               AuthenticationScheme = x.Name
           })
           .ToList();
        if (!providers.Any(x => x.AuthenticationScheme == provider)) {
            Logger.LogError("Invalid provider specified for authentication.");
            return await RedirectToErrorPageAsync(HttpContext, "Invalid provider", "Invalid provider specified for authentication.");
        }
        if (Url.IsLocalUrl(returnUrl) == false && Interaction.IsValidReturnUrl(returnUrl) == false) {
            Logger.LogError("Invalid return URL while federating to external provider.");
            return await RedirectToErrorPageAsync(HttpContext, "Invalid return URL.", "Invalid return URL while federating to external provider");
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
        }
        if (!Url.IsLocalUrl(returnUrl) && !Interaction.IsValidReturnUrl(returnUrl)) {
            Logger.LogError("Invalid return URL while federating to external provider.");
            return await RedirectToErrorPageAsync(HttpContext, "Invalid return URL.", "Invalid return URL while federating to external provider");
        }
        var externalLoginInfo = await SignInManager.GetExternalLoginInfoAsync() ?? throw new Exception($"Cannot read external login information from external provider.");
        var user = await UserManager.FindByLoginAsync(externalLoginInfo.LoginProvider, externalLoginInfo.ProviderKey);
        if (user is null) {
            return await UserNotFound(externalLoginInfo, returnUrl);
        }
        await Events.RaiseAsync(new ExtendedUserLoginSuccessEvent(externalLoginInfo.LoginProvider, externalLoginInfo.Principal.GetSubjectId(), user.Id, user.UserName!));
        // Save user tokes retrieved from external provider.
        await SignInManager.UpdateExternalAuthenticationTokensAsync(externalLoginInfo);
        var result = await SignInManager.ExternalLoginSignInAsync(externalLoginInfo.LoginProvider, externalLoginInfo.ProviderKey, isPersistent: true);

        await UserUpdateFromExternalInformation(user, externalLoginInfo);

        // Replace locale Claim only if it has a different value configured.
        var localeClaim = user.Claims.FirstOrDefault(x => x.ClaimType == JwtClaimTypes.Locale && x.ClaimValue == RequestCulture.Culture.TwoLetterISOLanguageName);
        if (localeClaim is null) {
            await UserManager.ReplaceClaimAsync(user, JwtClaimTypes.Locale, RequestCulture.Culture.TwoLetterISOLanguageName);
        }
        return await TryLogin(result, returnUrl);
    }

    /// <summary>This is called whenever a user is not found by an associated external identity provider.</summary>
    /// <param name="externalLoginInfo">Represents login information, source and externally source principal for a user record.</param>
    /// <param name="returnUrl">The return URL.</param>
    [NonAction]
    protected virtual async Task<IActionResult> UserNotFound(ExternalLoginInfo externalLoginInfo, string returnUrl) {
        await Task.CompletedTask;
        var claims = externalLoginInfo.Principal.Claims.ToList();
        TempData.Put("UserDetails", new AssociateViewModel {
            UserName = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Email)?.Value ?? claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Name)?.Value,
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
        IEventService events,
        IAuthenticationSchemeProvider schemeProvider,
        ILogger<ChallengeModel> logger
    ) : base(interaction, signInManager, userManager, events, schemeProvider, logger) { }
}
