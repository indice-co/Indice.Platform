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
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the login screen.</summary>
[AllowAnonymous]
[SecurityHeaders]
public class LoginPageModel : BasePageModel
{
    private readonly IClientStore _clientStore;
    private readonly IEventService _events;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IStringLocalizer<LoginPageModel> _localizer;
    private readonly ILogger<LoginPageModel> _logger;
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly ExtendedSignInManager<User> _signInManager;
    private readonly ExtendedUserManager<User> _userManager;

    /// <summary>Creates a new instance of <see cref="LoginPageModel"/> class.</summary>
    /// <param name="signInManager">Provides the APIs for user sign in.</param>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="schemeProvider">Responsible for managing what authentication schemes are supported.</param>
    /// <param name="clientStore">Retrieval of client configuration.</param>
    /// <param name="events">Interface for the event service.</param>
    /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
    /// <param name="logger">A generic interface for logging.</param>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="LoginPageModel"/>.</param>
    /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public LoginPageModel(
        ExtendedSignInManager<User> signInManager,
        ExtendedUserManager<User> userManager,
        IAuthenticationSchemeProvider schemeProvider,
        IClientStore clientStore,
        IEventService events,
        IIdentityServerInteractionService interaction,
        ILogger<LoginPageModel> logger,
        IStringLocalizer<LoginPageModel> localizer,
        IServiceProvider serviceProvider
    ) : base(serviceProvider) {
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _schemeProvider = schemeProvider ?? throw new ArgumentNullException(nameof(schemeProvider));
        _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>Login view model.</summary>
    public LoginViewModel ViewModel { get; set; }
    /// <summary>The current principal's username.</summary>
    public string UserName => User.FindFirstValue(JwtClaimTypes.Name);

    /// <summary>Login input model data.</summary>
    [BindProperty]
    public LoginInputModel Input { get; set; }

    /// <summary>Login page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public async Task<IActionResult> OnGetAsync([FromQuery] string returnUrl = null) {
        // Build a model so we know what to show on the login page.
        ViewModel = await BuildLoginViewModelAsync(returnUrl);
        if (ViewModel.PromptRegister()) {
            return RedirectToPage("Register", new { returnUrl });
        }
        if (ViewModel.IsExternalLoginOnly) {
            // We only have one option for logging in and it's an external provider.
            return RedirectToPage("Challenge", new {
                provider = ViewModel.ExternalLoginScheme,
                returnUrl,
                prompt = OidcConstants.PromptModes.SelectAccount
            });
        }
        return Page();
    }

    /// <summary>Login page POST handler.</summary>
    /// <param name="button"></param>
    /// <exception cref="Exception"></exception>
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync(string button) {
        // Check if we are in the context of an authorization request.
        var context = await _interaction.GetAuthorizationContextAsync(Input.ReturnUrl);
        // The user clicked the 'cancel' button.
        if (button != "login") {
            if (context != null) {
                // If the user cancels, send a result back into IdentityServer as if they denied the consent (even if this client does not require consent).
                // This will send back an access denied OIDC error response to the client.
                await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);
                // We can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null.
                if (context.IsNativeClient()) {
                    // The client is native, so this change in how to return the response is for better UX for the end user.
                    return this.LoadingPage("Redirect", Input.ReturnUrl);
                }
                return Redirect(Input.ReturnUrl);
            } else {
                // Since we don't have a valid context, then we just go back to the home page.
                return Redirect("~/");
            }
        }
        if (ModelState.IsValid) {
            // Validate username/password against database.
            var result = await _signInManager.PasswordSignInAsync(Input.UserName, Input.Password, AccountOptions.AllowRememberLogin && Input.RememberLogin, lockoutOnFailure: true);
            var user = await _userManager.FindByNameAsync(Input.UserName); ;
            if (result.Succeeded) {
                await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName));
                _logger.LogInformation("User '{UserName}' with email {Email} was successfully logged in.", user.UserName, user.Email);
                if (context != null) {
                    if (context.IsNativeClient()) {
                        // The client is native, so this change in how to return the response is for better UX for the end user.
                        return this.LoadingPage("Redirect", Input.ReturnUrl);
                    }
                    // We can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null.
                    return Redirect(Input.ReturnUrl);
                }
                // Request for a local page.
                if (_interaction.IsValidReturnUrl(Input.ReturnUrl) || Url.IsLocalUrl(Input.ReturnUrl)) {
                    return Redirect(Input.ReturnUrl);
                } else if (string.IsNullOrEmpty(Input.ReturnUrl)) {
                    return Redirect("/");
                } else {
                    // User might have clicked on a malicious link - should be logged.
                    _logger.LogError("User '{UserName}' might have clicked a malicious link during login: {ReturnUrl}.", UserName, Input.ReturnUrl);
                    throw new Exception("Invalid return URL.");
                }
            }
            if (result.IsLockedOut) {
                _logger.LogWarning("User '{UserName}' was locked out after {WrongLoginsAttemts} unsuccessful login attempts.", UserName, user?.AccessFailedCount);
                await _events.RaiseAsync(new UserLoginFailureEvent(Input.UserName, "User locked out."));
                ModelState.AddModelError(string.Empty, "Your account is temporarily locked. Please contact system administrator.");
            }
            var redirectResult = GetRedirectUrl(result, Input.ReturnUrl);
            if (redirectResult is not null) {
                await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName));
                return redirectResult;
            }
            _logger.LogWarning("User '{UserName}' entered invalid credentials during login.", UserName);
            await _events.RaiseAsync(new UserLoginFailureEvent(Input.UserName, "Invalid credentials."));
            ModelState.AddModelError(string.Empty, _localizer["Please check your credentials."]);
        } else {
            ModelState.AddModelError(string.Empty, _localizer["Please check your credentials."]);
        }
        // Something went wrong, show form with error.
        ViewModel = await BuildLoginViewModelAsync(Input);
        return Page();
    }

    private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl) {
        var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
        if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) is not null) {
            var local = context.IdP == IdentityServerConstants.LocalIdentityProvider;
            // This is meant to short circuit the UI and only trigger the one external IdP.
            var viewModel = new LoginViewModel {
                EnableLocalLogin = local,
                ReturnUrl = returnUrl,
                UserName = context?.LoginHint
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
        var schemes = await _schemeProvider.GetAllSchemesAsync();
        var providers = schemes
            .Where(x => x.DisplayName != null)
            .Select(x => new ExternalProviderModel {
                DisplayName = x.DisplayName ?? x.Name,
                AuthenticationScheme = x.Name
            })
            .ToList();
        var allowLocal = true;
        if (context?.Client.ClientId != null) {
            var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
            if (client != null) {
                allowLocal = client.EnableLocalLogin;
                if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any()) {
                    providers = providers.Where(provider => !client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                }
            }
        }
        return new LoginViewModel {
            AllowRememberLogin = AccountOptions.AllowRememberLogin,
            ClientId = context?.Client?.ClientId,
            EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
            ExternalProviders = providers.ToArray(),
            GenerateDeviceId = true,
            Operation = context?.Parameters?.AllKeys?.Contains(ExtraQueryParamNames.Operation) == true ? context?.Parameters[ExtraQueryParamNames.Operation] : null,
            ReturnUrl = returnUrl,
            UserName = context?.LoginHint
        };
    }

    /// <summary>Builds the <see cref="LoginViewModel"/> from the posted <see cref="LoginInputModel"/>.</summary>
    /// <param name="model">The request model.</param>
    public async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model) {
        var viewModel = await BuildLoginViewModelAsync(model.ReturnUrl);
        viewModel.UserName = model.UserName;
        viewModel.RememberLogin = model.RememberLogin;
        return viewModel;
    }
}
