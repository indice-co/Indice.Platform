using IdentityModel;
using IdentityServer4;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Indice.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the registration screen.</summary>
[AllowAnonymous]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public sealed class RegisterModel : PageModel
{
    private readonly ExtendedUserManager<User> _userManager;
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly IClientStore _clientStore;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly ILogger<RegisterModel> _logger;

    /// <summary></summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="schemeProvider">Responsible for managing what authentication schemes are supported.</param>
    /// <param name="clientStore">Retrieval of client configuration.</param>
    /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
    /// <param name="logger">A generic interface for logging.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public RegisterModel(
        ExtendedUserManager<User> userManager,
        IAuthenticationSchemeProvider schemeProvider,
        IClientStore clientStore,
        IIdentityServerInteractionService interaction,
        ILogger<RegisterModel> logger
    ) {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _schemeProvider = schemeProvider ?? throw new ArgumentNullException(nameof(schemeProvider));
        _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
        _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Registration input model data.</summary>
    [BindProperty]
    public RegisterInputModel Input { get; set; }

    /// <summary>List of external providers.</summary>
    public IEnumerable<ExternalProviderModel> ExternalProviders { get; set; } = new List<ExternalProviderModel>();
    /// <summary>Visible external providers are those given a <see cref="ExternalProviderModel.DisplayName"/></summary>
    public IEnumerable<ExternalProviderModel> VisibleExternalProviders => ExternalProviders.Where(x => !string.IsNullOrWhiteSpace(x.DisplayName));
    /// <summary>Optional flag that should hide the local user registration form and keep only the <see cref="ExternalProviders"/> options.</summary>
    public bool ExternalRegistrationOnly { get; set; }
    /// <summary>The authentication scheme of the external registration.</summary>
    public string ExternalRegistrationScheme => ExternalProviders?.SingleOrDefault()?.AuthenticationScheme;
    /// <summary>The return URL is used to keep track of the original intent of the user when he landed on login and switched over to register.</summary>
    public string ReturnUrl { get; set; }

    /// <summary>Registration page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public async Task<IActionResult> OnGetAsync(string returnUrl = null) {
        await BuildRegisterModelAsync(returnUrl);
        if (ExternalRegistrationOnly) {
            return RedirectToPage("/external/challenge", new { provider = ExternalRegistrationScheme, returnUrl });
        }
        return Page();
    }

    /// <summary>Registration page POST handler.</summary>
    public async Task<IActionResult> OnPostAsync() {
        if (!ModelState.IsValid) {
            return Page();
        }
        var user = CreateUserFromInput();
        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded) {
            //var viewModel = await _accountService.BuildRegisterViewModelAsync(request);
            //GetErrorResult(result);
            return Page();
        }
        //await SendConfirmationEmail(user, ReturnUrl);
        _logger.LogInformation(3, "User created a new account with password.");
        if (_interaction.IsValidReturnUrl(ReturnUrl) || Url.IsLocalUrl(ReturnUrl)) {
            return RedirectToPage("/login", new { returnUrl = ReturnUrl });
        }
        return RedirectToPage("/login");
    }

    private async Task BuildRegisterModelAsync(string returnUrl) {
        ReturnUrl = returnUrl;
        Input = new RegisterInputModel();
        var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
        if (context?.IdP is not null && await _schemeProvider.GetSchemeAsync(context.IdP) is not null) {
            var local = context.IdP == IdentityServerConstants.LocalIdentityProvider;
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
        if (context?.Client.ClientId != null) {
            var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
            if (client != null) {
                allowLocal = client.EnableLocalLogin;
                if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any()) {
                    providers = providers.Where(provider => !client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                }
            }
        }
        Input.UserName = context?.LoginHint;
        ExternalProviders = providers.ToArray();
        Input.ClientId = context?.Client?.ClientId;
    }

    private User CreateUserFromInput() {
        var user = new User {
            UserName = Input.UserName,
            Email = Input.Email,
            PhoneNumber = Input.PhoneNumber
        };
        if (!string.IsNullOrWhiteSpace(Input.FirstName)) {
            user.Claims.Add(new() {
                ClaimType = JwtClaimTypes.GivenName,
                ClaimValue = Input.FirstName,
                UserId = user.Id
            });
        }
        if (!string.IsNullOrWhiteSpace(Input.LastName)) {
            user.Claims.Add(new() {
                ClaimType = JwtClaimTypes.FamilyName,
                ClaimValue = Input.LastName,
                UserId = user.Id
            });
        }
        user.Claims.Add(new() {
            ClaimType = BasicClaimTypes.ConsentCommencial,
            ClaimValue = Input.HasAcceptedTerms ? bool.TrueString.ToLower() : bool.FalseString.ToLower(),
            UserId = user.Id
        });
        user.Claims.Add(new() {
            ClaimType = BasicClaimTypes.ConsentTerms,
            ClaimValue = Input.HasReadPrivacyPolicy ? bool.TrueString.ToLower() : bool.FalseString.ToLower(),
            UserId = user.Id
        });
        user.Claims.Add(new() {
            ClaimType = BasicClaimTypes.ConsentTermsDate,
            ClaimValue = $"{DateTime.UtcNow:O}",
            UserId = user.Id
        });
        user.Claims.Add(new() {
            ClaimType = BasicClaimTypes.ConsentCommencialDate,
            ClaimValue = $"{DateTime.UtcNow:O}",
            UserId = user.Id
        });
        foreach (var attribute in Input.Claims) {
            if (string.IsNullOrWhiteSpace(attribute.Value)) {
                continue;
            }
            user.Claims.Add(new() {
                ClaimType = attribute.Name,
                ClaimValue = attribute.Value,
                UserId = user.Id
            });
        }
        return user;
    }
}
