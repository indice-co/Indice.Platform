using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.AspNetCore.Identity.Models;
using Indice.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Identity
{
    /// <inheritdoc />
    public class AccountService : IAccountService
    {
        private readonly IClientStore _clientStore;
        private readonly ExtendedUserManager<User> _userManager;
        private readonly ExtendedSignInManager<User> _signInManager;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthenticationSchemeProvider _schemeProvider;

        /// <summary>Constructs the <see cref="AccountService"/>.</summary>
        /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
        /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>.</param>
        /// <param name="schemeProvider">Responsible for managing what authenticationSchemes are supported.</param>
        /// <param name="clientStore">Retrieval of client configuration.</param>
        /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
        /// <param name="signInManager">Provides the APIs for user sign in.</param>
        public AccountService(
            IIdentityServerInteractionService interaction,
            IHttpContextAccessor httpContextAccessor,
            IAuthenticationSchemeProvider schemeProvider,
            IClientStore clientStore,
            ExtendedUserManager<User> userManager,
            ExtendedSignInManager<User> signInManager
        ) {
            _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _schemeProvider = schemeProvider ?? throw new ArgumentNullException(nameof(schemeProvider));
            _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        }

        /// <inheritdoc />
        public async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl) {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null) {
                var local = context.IdP == IdentityServerConstants.LocalIdentityProvider;
                // This is meant to short circuit the UI and only trigger the one external IdP.
                var viewModel = new LoginViewModel {
                    EnableLocalLogin = local,
                    ReturnUrl = returnUrl,
                    UserName = context?.LoginHint
                };
                if (!local) {
                    viewModel.ExternalProviders = new[] {
                        new ExternalProvider {
                            AuthenticationScheme = context.IdP
                        }
                    };
                }
                return viewModel;
            }
            var schemes = await _schemeProvider.GetAllSchemesAsync();
            var providers = schemes
                .Where(x => x.DisplayName != null)
                .Select(x => new ExternalProvider {
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
                EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
                ReturnUrl = returnUrl,
                UserName = context?.LoginHint,
                ExternalProviders = providers.ToArray(),
                ClientId = context?.Client?.ClientId,
                Operation = context?.Parameters?.AllKeys?.Contains(ExtraQueryParamNames.Operation) == true ? context?.Parameters[ExtraQueryParamNames.Operation] : null
            };
        }

        /// <inheritdoc />
        public async Task<MfaLoginViewModel> BuildMfaLoginViewModelAsync(string returnUrl) {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user is null) {
                return default;
            }
            var trustedDevicesCount = await _userManager.GetTrustedDevicesCountAsync(user);
            var deliveryChannel = TotpDeliveryChannel.None;
            if (trustedDevicesCount > 0) {
                deliveryChannel = TotpDeliveryChannel.PushNotification;
            } else {
                var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
                var phoneNumberConfirmed = !string.IsNullOrWhiteSpace(phoneNumber) && await _userManager.IsPhoneNumberConfirmedAsync(user);
                if (phoneNumberConfirmed) {
                    deliveryChannel = TotpDeliveryChannel.Sms;
                }
            }
            return new MfaLoginViewModel {
                DeliveryChannel = deliveryChannel,
                ReturnUrl = returnUrl,
                User = user
            };
        }

        /// <inheritdoc />
        public async Task<TRegisterViewModel> BuildRegisterViewModelAsync<TRegisterViewModel>(string returnUrl) where TRegisterViewModel : RegisterViewModel, new() {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null) {
                var local = context.IdP == IdentityServerConstants.LocalIdentityProvider;
                // This is meant to short circuit the UI and only trigger the one external IdP.
                var viewModel = new TRegisterViewModel {
                    ReturnUrl = returnUrl,
                    UserName = context?.LoginHint,
                };
                if (!local) {
                    viewModel.ExternalProviders = new[] {
                        new ExternalProvider {
                            AuthenticationScheme = context.IdP
                        }
                    };
                }
                return viewModel;
            }
            var schemes = await _schemeProvider.GetAllSchemesAsync();
            var providers = schemes
                .Where(x => x.DisplayName != null)
                .Select(x => new ExternalProvider {
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
            return new TRegisterViewModel {
                ReturnUrl = returnUrl,
                UserName = context?.LoginHint,
                ExternalProviders = providers.ToArray(),
                ClientId = context?.Client?.ClientId
            };
        }

        /// <inheritdoc />
        public async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId) {
            var viewModel = new LogoutViewModel {
                LogoutId = logoutId,
                ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt
            };
            var context = await _interaction.GetLogoutContextAsync(logoutId);
            viewModel.ClientId = context?.ClientId;
            if (context?.ShowSignoutPrompt == false) {
                // it's safe to automatically sign-out.
                viewModel.ShowLogoutPrompt = false;
                return viewModel;
            }
            // Show the logout prompt. this prevents attacks where the user is automatically signed out by another malicious web page.
            return viewModel;
        }

        /// <inheritdoc />
        public async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId) {
            // Get context information (client name, post logout redirect URI and iframe for federated sign out).
            var logout = await _interaction.GetLogoutContextAsync(logoutId);
            var viewModel = new LoggedOutViewModel {
                AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientId = logout?.ClientId,
                ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
                SignOutIframeUrl = logout?.SignOutIFrameUrl,
                LogoutId = logoutId
            };
            var user = _httpContextAccessor.HttpContext.User;
            if (user?.Identity.IsAuthenticated == true) {
                var idp = user.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
                if (idp != null && idp != IdentityServerConstants.LocalIdentityProvider) {
                    var providerSupportsSignout = await _httpContextAccessor.HttpContext.GetSchemeSupportsSignOutAsync(idp);
                    if (providerSupportsSignout) {
                        if (viewModel.LogoutId == null) {
                            // If there's no current logout context, we need to create one. This captures necessary info from the current logged in user before we sign out and redirect away to the external IdP for sign out.
                            viewModel.LogoutId = await _interaction.CreateLogoutContextAsync();
                        }
                        viewModel.ExternalAuthenticationScheme = idp;
                    }
                }
            }
            return viewModel;
        }
    }
}
