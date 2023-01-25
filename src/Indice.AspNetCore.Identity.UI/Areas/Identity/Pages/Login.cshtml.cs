using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.AspNetCore.Identity.Extensions;
using Indice.AspNetCore.Identity.Models;
using Indice.AspNetCore.Identity.UI.Areas.Identity.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Identity.UI.Areas.Identity.Pages
{
    /// <summary>Page model for the login screen.</summary>
    [AllowAnonymous]
    [SecurityHeaders]
    public class LoginModel : PageModel
    {
        private readonly ExtendedSignInManager<User> _signInManager;
        private readonly ExtendedUserManager<User> _userManager;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IClientStore _clientStore;
        private readonly IEventService _events;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly ILogger<LoginModel> _logger;
        private readonly IStringLocalizer<LoginModel> _localizer;

        /// <summary>Creates a new instance of <see cref="LoginModel"/> class.</summary>
        /// <param name="signInManager">Provides the APIs for user sign in.</param>
        /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
        /// <param name="schemeProvider">Responsible for managing what authentication schemes are supported.</param>
        /// <param name="clientStore">Retrieval of client configuration.</param>
        /// <param name="events">Interface for the event service.</param>
        /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
        /// <param name="logger">A generic interface for logging.</param>
        /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="LoginModel"/>.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public LoginModel(
            ExtendedSignInManager<User> signInManager,
            ExtendedUserManager<User> userManager,
            IAuthenticationSchemeProvider schemeProvider,
            IClientStore clientStore,
            IEventService events,
            IIdentityServerInteractionService interaction,
            ILogger<LoginModel> logger,
            IStringLocalizer<LoginModel> localizer
        ) {
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _schemeProvider = schemeProvider ?? throw new ArgumentNullException(nameof(schemeProvider));
            _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>Login input model data.</summary>
        [BindProperty]
        public LoginInputModelTemp Input { get; set; }

        /// <summary>Allow remember login.</summary>
        public bool AllowRememberLogin { get; set; } = true;
        /// <summary>Enables local logins (if false only external provider list will be available).</summary>
        public bool EnableLocalLogin { get; set; } = true;
        /// <summary>List of external providers.</summary>
        public IEnumerable<ExternalProviderModel> ExternalProviders { get; set; } = Enumerable.Empty<ExternalProviderModel>();
        /// <summary>The visible external providers are those form the <see cref="ExternalProviders"/> list that have a display name.</summary>
        public IEnumerable<ExternalProviderModel> VisibleExternalProviders => ExternalProviders.Where(x => !string.IsNullOrWhiteSpace(x.DisplayName));
        /// <summary>Use this flag to hide the local login form.</summary>
        public bool IsExternalLoginOnly => EnableLocalLogin == false && ExternalProviders?.Count() == 1;
        /// <summary>The scheme to use for external login cookie.</summary>
        public string ExternalLoginScheme => IsExternalLoginOnly ? ExternalProviders?.SingleOrDefault()?.AuthenticationScheme : null;
        /// <summary>A direction to display a different screen when a client asks for the authorize endpoint.</summary>
        /// <remarks>Use the 'operation={operation_name}' query parameter on the authorize endpoint.</remarks>
        public string Operation { get; set; }
        /// <summary>Specifies whether a device (browser) id should be generated.</summary>
        public bool GenerateDeviceId { get; set; }
        /// <summary>The return URL after the login is successful.</summary>
        public string ReturnUrl { get; set; }

        /// <summary>Login page GET handler.</summary>
        /// <param name="returnUrl">The return URL.</param>
        public async Task<IActionResult> OnGetAsync(string returnUrl = null) {
            await BuildLoginModelAsync(returnUrl);
            if (PromptRegister()) {
                return RedirectToPage("register");
            }
            if (IsExternalLoginOnly) {
                return RedirectToPage("/external/challenge", new {
                    provider = ExternalLoginScheme,
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
            var context = await _interaction.GetAuthorizationContextAsync(ReturnUrl);
            if (button != "login") {
                if (context != null) {
                    // If the user cancels, send a result back into IdentityServer as if they denied the consent (even if this client does not require consent).
                    // This will send back an access denied OIDC error response to the client.
                    await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);
                    // We can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null.
                    if (context.IsNativeClient()) {
                        // The client is native, so this change in how to return the response is for better UX for the end user.
                        //return this.LoadingPage("Redirect", ReturnUrl);
                    }
                    return Redirect(ReturnUrl);
                } else {
                    // Since we don't have a valid context, then we just go back to the home page.
                    return Redirect("~/");
                }
            }
            if (ModelState.IsValid) {
                // Validate username/password against database.
                var result = await _signInManager.PasswordSignInAsync(Input.UserName, Input.Password, AccountOptions.AllowRememberLogin && Input.RememberLogin, lockoutOnFailure: true);
                User user = null;
                if (result.Succeeded) {
                    user = await _userManager.FindByNameAsync(Input.UserName);
                    await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName));
                    _logger.LogInformation("User '{UserName}' with email {Email} was successfully logged in.", user.UserName, user.Email);
                    if (context != null) {
                        if (context.IsNativeClient()) {
                            // The client is native, so this change in how to return the response is for better UX for the end user.
                            //return this.LoadingPage("Redirect", ReturnUrl);
                        }
                        // We can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null.
                        return Redirect(ReturnUrl);
                    }
                    // Request for a local page.
                    if (_interaction.IsValidReturnUrl(ReturnUrl) || Url.IsLocalUrl(ReturnUrl)) {
                        return Redirect(ReturnUrl);
                    } else if (string.IsNullOrEmpty(ReturnUrl)) {
                        return Redirect("~/");
                    } else {
                        // User might have clicked on a malicious link - should be logged.
                        _logger.LogError("User '{UserName}' might have clicked a malicious link during login: {ReturnUrl}.", Input.UserName, ReturnUrl);
                        throw new Exception("Invalid return URL.");
                    }
                }
                if (result.RequiresPasswordChange()) {
                    return RedirectToPage("/login/password-expired", new { ReturnUrl });
                } else if (result.RequiresEmailConfirmation()) {
                    return RedirectToPage("/login/verify-email", new { ReturnUrl });
                } else if (result.RequiresPhoneNumberConfirmation()) {
                    return RedirectToPage("/login/verify-phone", new { ReturnUrl });
                } else if (result.IsLockedOut) {
                    _logger.LogWarning("User '{UserName}' was locked out after {WrongLoginsNumber} unsuccessful login attempts.", Input.UserName, user?.AccessFailedCount);
                    await _events.RaiseAsync(new UserLoginFailureEvent(Input.UserName, "User locked out."));
                    ModelState.AddModelError(string.Empty, "Your account is temporarily locked. Please contact system administrator.");
                } else if (result.RequiresTwoFactor) {
                    return RedirectToPage("/login/mfa", new { ReturnUrl });
                } else {
                    _logger.LogWarning("User '{UserName}' entered invalid credentials during login.", Input.UserName);
                    await _events.RaiseAsync(new UserLoginFailureEvent(Input.UserName, "Invalid credentials."));
                    ModelState.AddModelError(string.Empty, _localizer["Please check your credentials."]);
                }
            } else {
                ModelState.AddModelError(string.Empty, _localizer["Please check your credentials."]);
            }
            // Something went wrong, show form with error.
            //var viewModel = await _accountService.BuildLoginViewModelAsync(model);
            return Page();
        }

        private bool PromptRegister() => Operation?.Equals("register", StringComparison.OrdinalIgnoreCase) == true;

        private async Task BuildLoginModelAsync(string returnUrl) {
            ReturnUrl = returnUrl;
            Input = new LoginInputModelTemp();
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null) {
                var local = context.IdP == IdentityServerConstants.LocalIdentityProvider;
                EnableLocalLogin = local;
                ReturnUrl = returnUrl;
                Input.UserName = context?.LoginHint;
                if (!local) {
                    ExternalProviders = new[] {
                        new ExternalProviderModel {
                            AuthenticationScheme = context.IdP
                        }
                    };
                }
                return;
            }
            var schemes = await _schemeProvider.GetAllSchemesAsync();
            var providers = schemes
                .Where(x => x.DisplayName is not null)
                .Select(x => new ExternalProviderModel {
                    DisplayName = x.DisplayName ?? x.Name,
                    AuthenticationScheme = x.Name
                })
                .ToList();
            var allowLocal = true;
            if (context?.Client.ClientId is not null) {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
                if (client != null) {
                    allowLocal = client.EnableLocalLogin;
                    if (client.IdentityProviderRestrictions is not null && client.IdentityProviderRestrictions.Any()) {
                        providers = providers.Where(provider => !client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                    }
                }
            }
            AllowRememberLogin = AccountOptions.AllowRememberLogin;
            Input.ClientId = context?.Client?.ClientId;
            EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin;
            ExternalProviders = providers.ToArray();
            GenerateDeviceId = true;
            Operation = context?.Parameters?.AllKeys?.Contains(ExtraQueryParamNames.Operation) == true ? context?.Parameters[ExtraQueryParamNames.Operation] : null;
            ReturnUrl = returnUrl;
            Input.UserName = context?.LoginHint;
        }
    }
}
