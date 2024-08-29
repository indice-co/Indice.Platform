using System.Security.Claims;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the login screen.</summary>
[AllowAnonymous]
[IdentityUI(typeof(LoginModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseLoginModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseLoginModel"/> class.</summary>
    /// <param name="signInManager">Provides the APIs for user sign in.</param>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="schemeProvider">Responsible for managing what authentication schemes are supported.</param>
    /// <param name="clientStore">Retrieval of client configuration.</param>
    /// <param name="events">Interface for the event service.</param>
    /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
    /// <param name="logger">A generic interface for logging.</param>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="BaseLoginModel"/>.</param>
    /// <param name="identityUiOptions">Configuration options for Identity UI.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseLoginModel(
        ExtendedSignInManager<User> signInManager,
        ExtendedUserManager<User> userManager,
        IAuthenticationSchemeProvider schemeProvider,
        IClientStore clientStore,
        IEventService events,
        IIdentityServerInteractionService interaction,
        ILogger<BaseLoginModel> logger,
        IStringLocalizer localizer,
        IOptions<IdentityUIOptions> identityUiOptions
    ) : base() {
        SignInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        SchemeProvider = schemeProvider ?? throw new ArgumentNullException(nameof(schemeProvider));
        ClientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        IdentityUIOptions = identityUiOptions?.Value ?? throw new ArgumentNullException(nameof(identityUiOptions));
        Localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>Retrieval of client configuration.</summary>
    protected IClientStore ClientStore { get; }
    /// <summary>Interface for the event service.</summary>
    protected IEventService Events { get; }
    /// <summary>Provide services be used by the user interface to communicate with IdentityServer.</summary>
    protected IIdentityServerInteractionService Interaction { get; }
    /// <summary>A generic interface for logging.</summary>
    protected ILogger<BaseLoginModel> Logger { get; }
    /// <summary>Responsible for managing what authentication schemes are supported.</summary>
    protected IAuthenticationSchemeProvider SchemeProvider { get; }
    /// <summary>Provides the APIs for user sign in.</summary>
    protected ExtendedSignInManager<User> SignInManager { get; }
    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>Configuration options for Identity UI.</summary>
    protected IdentityUIOptions IdentityUIOptions { get; }
    /// <summary>Login String Localizer.</summary>
    protected IStringLocalizer Localizer { get; }
    /// <summary>The current principal's username.</summary>
    public string? UserName => User.FindFirstValue(JwtClaimTypes.Name);
    /// <summary>Login view model.</summary>
    public LoginViewModel View { get; set; } = new LoginViewModel();

    /// <summary>Login input model data.</summary>
    [BindProperty]
    public LoginInputModel Input { get; set; } = new LoginInputModel();

    /// <summary>Login page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnGetAsync(string? returnUrl = null) {
        UserManager.StateProvider.ClearState();
        // Build a model so we know what to show on the login page.
        Input = View = await BuildLoginViewModelAsync(returnUrl);
        if (View.PromptRegister()) {
            return RedirectToPage(UiOptions.OnBoardingPage, new { returnUrl });
        }
        if (View.IsExternalLoginOnly) {
            // We only have one option for logging in and it's an external provider.
            return RedirectToPage("/Challenge", new {
                provider = View.ExternalLoginScheme,
                returnUrl,
                prompt = OidcConstants.PromptModes.SelectAccount
            });
        }
        return Page();
    }

    /// <summary>Login page POST handler.</summary>
    /// <param name="button">The value of the button pressed.</param>
    /// <exception cref="Exception"></exception>
    public virtual async Task<IActionResult> OnPostAsync(string button) {
        // Check if we are in the context of an authorization request.
        var context = await Interaction.GetAuthorizationContextAsync(Input.ReturnUrl);
        // The user clicked the 'cancel' button.
        if (button is not "login") {
            if (context is not null) {
                // If the user cancels, send a result back into IdentityServer as if they denied the consent (even if this client does not require consent).
                // This will send back an access denied OIDC error response to the client.
                await Interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);
                // We can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null.
                if (context.IsNativeClient()) {
                    // The client is native, so this change in how to return the response is for better UX for the end user.
                    return this.LoadingPage("Redirect", Input.ReturnUrl ?? "/");
                }
                return Redirect(Input.ReturnUrl ?? "/");
            } else {
                // Since we don't have a valid context, then we just go back to the home page.
                return Redirect("/");
            }
        }
        if (ModelState.IsValid) {
            // Validate username/password against database.
            var result = await SignInManager.PasswordSignInAsync(Input.UserName!, Input.Password!, IdentityUIOptions.AllowRememberLogin && Input.RememberLogin, lockoutOnFailure: true);
            var user = await UserManager.FindByNameAsync(Input.UserName!);
            if (result.Succeeded && user is not null) {
                // Replace locale Claim only if it has a different value configured.
                var localeClaim = user.Claims.FirstOrDefault(x => x.ClaimType == JwtClaimTypes.Locale && x.ClaimValue == RequestCulture.Culture.TwoLetterISOLanguageName);
                if (localeClaim is null) {
                    await UserManager.ReplaceClaimAsync(user, JwtClaimTypes.Locale, RequestCulture.Culture.TwoLetterISOLanguageName);
                }
                Logger.LogInformation("User '{UserName}' with email {Email} was successfully logged in.", user.UserName, user.Email);
                if (context is not null) {
                    if (context.IsNativeClient()) {
                        // The client is native, so this change in how to return the response is for better UX for the end user.
                        return this.LoadingPage("Redirect", Input.ReturnUrl ?? "/");
                    }
                    // We can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null.
                    return Redirect(Input.ReturnUrl ?? "/");
                }
                // Request for a local page.
                if (string.IsNullOrEmpty(Input.ReturnUrl)) {
                    return Redirect("/");
                } else if (IsValidReturnUrl(Input.ReturnUrl)) {
                    return Redirect(Input.ReturnUrl);
                } else {
                    // User might have clicked on a malicious link - should be logged.
                    Logger.LogError("User '{UserName}' might have clicked a malicious link during login: {ReturnUrl}.", Input.UserName!, Input.ReturnUrl);
                    throw new Exception("Invalid return URL.");
                }
            }
            if (result.IsLockedOut) {
                Logger.LogWarning("User '{UserName}' was locked out after {WrongLoginsAttempts} unsuccessful login attempts.", Input.UserName, user?.AccessFailedCount);
                await Events.RaiseAsync(new ExtendedUserLoginFailureEvent(Input.UserName!, "User locked out.", subjectId: user?.Id, clientId: context?.Client?.ClientId, clientName: context?.Client?.ClientName));
                ModelState.AddModelError(string.Empty, "Your account is temporarily locked. Please contact system administrator.");
            }
            var redirectUrl = GetRedirectUrl(result, Input.ReturnUrl);
            if (redirectUrl is not null && user is not null) {
                return Redirect(redirectUrl);
            }
            Logger.LogWarning("User '{UserName}' entered invalid credentials during login.", Input.UserName);
            await Events.RaiseAsync(new ExtendedUserLoginFailureEvent(Input.UserName!, "Invalid credentials.", subjectId: user?.Id, clientId: context?.Client?.ClientId, clientName: context?.Client?.ClientName));
            ModelState.AddModelError(string.Empty, Localizer["Please check your credentials."]);
        } else {
            ModelState.AddModelError(string.Empty, Localizer["Please check your credentials."]);
        }
        // Something went wrong, show form with error.
        View = await BuildLoginViewModelAsync(Input);
        return Page();
    }

    private async Task<LoginViewModel> BuildLoginViewModelAsync(string? returnUrl) {
        var context = await Interaction.GetAuthorizationContextAsync(returnUrl);
        if (context?.IdP is not null && await SchemeProvider.GetSchemeAsync(context.IdP) is not null) {
            var local = context.IdP == IdentityServerConstants.LocalIdentityProvider;
            // This is meant to short circuit the UI and only trigger the one external IdP.
            var viewModel = new LoginViewModel {
                EnableLocalLogin = local,
                ReturnUrl = returnUrl ?? "/",
                UserName = context.LoginHint
            };
            if (!local) {
                viewModel.ExternalProviders = new[] {
                    new ExternalProviderModel {
                        AuthenticationScheme = context.IdP
                    }
                };
            }
            return viewModel;
        }
        var schemes = await SchemeProvider.GetAllSchemesAsync();
        var providers = schemes
            .Where(x => x.DisplayName is not null)
            .Select(x => new ExternalProviderModel {
                DisplayName = x.DisplayName ?? x.Name,
                AuthenticationScheme = x.Name
            })
            .ToList();
        var allowLocal = true;
        if (context?.Client.ClientId is not null) {
            var client = await ClientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
            if (client is not null) {
                allowLocal = client.EnableLocalLogin;
                if (client.IdentityProviderRestrictions is not null && client.IdentityProviderRestrictions.Any()) {
                    providers = providers.Where(provider => !client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                }
            }
        }
        return new LoginViewModel {
            AllowRememberLogin = IdentityUIOptions.AllowRememberLogin,
            ClientId = context?.Client?.ClientId,
            EnableLocalLogin = allowLocal && IdentityUIOptions.EnableLocalLogin,
            ExternalProviders = providers.ToArray(),
            GenerateDeviceId = true,
            Operation = context?.Parameters?.AllKeys?.Contains(ExtraQueryParamNames.Operation) == true
                ? context?.Parameters[ExtraQueryParamNames.Operation]
                : null,
            ReturnUrl = returnUrl,
            UserName = context?.LoginHint
        };
    }

    private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model) {
        var viewModel = await BuildLoginViewModelAsync(model.ReturnUrl);
        viewModel.UserName = model.UserName;
        viewModel.RememberLogin = model.RememberLogin;
        return viewModel;
    }
}

internal class LoginModel : BaseLoginModel
{
    public LoginModel(
        ExtendedSignInManager<User> signInManager,
        ExtendedUserManager<User> userManager,
        IAuthenticationSchemeProvider schemeProvider,
        IClientStore clientStore,
        IEventService events,
        IIdentityServerInteractionService interaction,
        ILogger<LoginModel> logger,
        IStringLocalizer<LoginModel> localizer,
        IOptions<IdentityUIOptions> identityUiOptions
    ) : base(signInManager, userManager, schemeProvider, clientStore, events, interaction, logger, localizer, identityUiOptions) { }
}
