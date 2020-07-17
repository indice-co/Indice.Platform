using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Identity.Services
{
    /// <summary>
    /// Account service wraps account controllers operations regarding creating and validating viewmodels.
    /// </summary>
    public class AccountService
    {
        private readonly IClientStore _clientStore;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthenticationSchemeProvider _schemeProvider;

        /// <summary>
        /// Constructs the <see cref="AccountService"/>
        /// </summary>
        /// <param name="interaction"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="schemeProvider"></param>
        /// <param name="clientStore"></param>
        public AccountService(IIdentityServerInteractionService interaction, IHttpContextAccessor httpContextAccessor, IAuthenticationSchemeProvider schemeProvider, IClientStore clientStore) {
            _interaction = interaction;
            _httpContextAccessor = httpContextAccessor;
            _schemeProvider = schemeProvider;
            _clientStore = clientStore;
        }

        /// <summary>
        /// Builds the <see cref="LoginViewModel"/>.
        /// </summary>
        /// <param name="returnUrl">The return url to go to after successful login</param>
        public async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl) {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null) {
                var local = context.IdP == IdentityServer4.IdentityServerConstants.LocalIdentityProvider;

                // this is meant to short circuit the UI and only trigger the one external IdP
                var vm = new LoginViewModel {
                    EnableLocalLogin = local,
                    ReturnUrl = returnUrl,
                    Username = context?.LoginHint,
                };

                if (!local) {
                    vm.ExternalProviders = new[] { new ExternalProvider { AuthenticationScheme = context.IdP } };
                }

                return vm;
            }

            var schemes = await _schemeProvider.GetAllSchemesAsync();

            var providers = schemes
                .Where(x => x.DisplayName != null)
                .Select(x => new ExternalProvider {
                    DisplayName = x.DisplayName ?? x.Name,
                    AuthenticationScheme = x.Name
                }).ToList();

            var allowLocal = true;
            if (context?.Client.ClientId != null) {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
                if (client != null) {
                    allowLocal = client.EnableLocalLogin;

                    if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any()) {
                        providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                    }
                }
            }

            return new LoginViewModel {
                AllowRememberLogin = AccountOptions.AllowRememberLogin,
                EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
                ReturnUrl = returnUrl,
                Username = context?.LoginHint,
                ExternalProviders = providers.ToArray(),
                ClientId = context?.Client?.ClientId
            };
        }

        /// <summary>
        /// Builds the <see cref="LoginViewModel"/> from the posted request <see cref="LoginInputModel"/>.
        /// </summary>
        /// <param name="model">the request model.</param>
        public async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model) {
            var viewModel = await BuildLoginViewModelAsync(model.ReturnUrl);
            viewModel.Username = model.Username;
            viewModel.RememberLogin = model.RememberLogin;
            return viewModel;
        }

        /// <summary>
        /// Builds the <see cref="RegisterViewModel"/>.
        /// </summary>
        /// <param name="returnUrl"></param>
        public async Task<RegisterViewModel> BuildRegisterViewModelAsync(string returnUrl) => await BuildRegisterViewModelAsync<RegisterViewModel>(returnUrl);

        /// <summary>
        /// Generic counterpart in case someone extends the basic <see cref="RegisterViewModel"/> with extra properties.
        /// </summary>
        /// <typeparam name="TRegisterViewModel"></typeparam>
        /// <param name="returnUrl"></param>
        public async Task<TRegisterViewModel> BuildRegisterViewModelAsync<TRegisterViewModel>(string returnUrl) where TRegisterViewModel : RegisterViewModel, new() {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null) {
                var local = context.IdP == IdentityServer4.IdentityServerConstants.LocalIdentityProvider;

                // this is meant to short circuit the UI and only trigger the one external IdP
                var vm = new TRegisterViewModel {
                    //EnableLocalLogin = local,
                    ReturnUrl = returnUrl,
                    Username = context?.LoginHint,
                };

                if (!local) {
                    vm.ExternalProviders = new[] { new ExternalProvider { AuthenticationScheme = context.IdP } };
                }

                return vm;
            }

            var schemes = await _schemeProvider.GetAllSchemesAsync();

            var providers = schemes
                .Where(x => x.DisplayName != null)
                .Select(x => new ExternalProvider {
                    DisplayName = x.DisplayName ?? x.Name,
                    AuthenticationScheme = x.Name
                }).ToList();

            var allowLocal = true;
            if (context?.Client.ClientId != null) {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
                if (client != null) {
                    allowLocal = client.EnableLocalLogin;

                    if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any()) {
                        providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                    }
                }
            }

            return new TRegisterViewModel {
                ReturnUrl = returnUrl,
                Username = context?.LoginHint,
                ExternalProviders = providers.ToArray(),
                ClientId = context?.Client?.ClientId
            };
        }

        /// <summary>
        /// Builds the <see cref="RegisterViewModel"/> from the posted request <see cref="RegisterRequest"/>.
        /// </summary>
        /// <param name="model"></param>
        public async Task<RegisterViewModel> BuildRegisterViewModelAsync(RegisterRequest model) => await BuildRegisterViewModelAsync<RegisterViewModel>(model);

        /// <summary>
        ///  Builds the <see cref="RegisterViewModel"/> from the posted request <see cref="RegisterRequest"/>.
        ///  Generic counterpart in case someone extends the basic <see cref="RegisterViewModel"/> with extra properties.
        /// </summary>
        /// <typeparam name="TRegisterViewModel"></typeparam>
        /// <param name="model"></param>
        public async Task<TRegisterViewModel> BuildRegisterViewModelAsync<TRegisterViewModel>(RegisterRequest model) where TRegisterViewModel : RegisterViewModel, new() {
            var viewModel = await BuildRegisterViewModelAsync<TRegisterViewModel>(model.ReturnUrl);
            viewModel.Username = model.Username;
            return viewModel;
        }

        /// <summary>
        /// Builds the logout viewmodel.
        /// </summary>
        /// <param name="logoutId"></param>
        public async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId) {
            var user = _httpContextAccessor.HttpContext.User;
            var vm = new LogoutViewModel { 
                LogoutId = logoutId, 
                ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt 
            };

            if (user?.Identity.IsAuthenticated != true) {
                // if the user is not authenticated, then just show logged out page
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);
            vm.ClientId = context?.ClientId;
            if (context?.ShowSignoutPrompt == false) {
                // it's safe to automatically sign-out
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.
            return vm;
        }

        /// <summary>
        /// Build the post logout viewmodel. <see cref="LoggedOutViewModel"/>
        /// </summary>
        /// <param name="logoutId"></param>
        public async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId) {
            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interaction.GetLogoutContextAsync(logoutId);

            var vm = new LoggedOutViewModel {
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
                if (idp != null && idp != IdentityServer4.IdentityServerConstants.LocalIdentityProvider) {
                    var providerSupportsSignout = await _httpContextAccessor.HttpContext.GetSchemeSupportsSignOutAsync(idp);
                    if (providerSupportsSignout) {
                        if (vm.LogoutId == null) {
                            // if there's no current logout context, we need to create one
                            // this captures necessary info from the current logged in user
                            // before we signout and redirect away to the external IdP for signout
                            vm.LogoutId = await _interaction.CreateLogoutContextAsync();
                        }

                        vm.ExternalAuthenticationScheme = idp;
                    }
                }
            }

            return vm;
        }
    }
}
