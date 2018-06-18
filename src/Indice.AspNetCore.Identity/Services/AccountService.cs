using System.Linq;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Identity.Services
{
    public class AccountService
    {
        private readonly IClientStore _clientStore;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private IAuthenticationSchemeProvider _schemeProvider;

        public AccountService(IIdentityServerInteractionService interaction, IHttpContextAccessor httpContextAccessor, IAuthenticationSchemeProvider schemeProvider, IClientStore clientStore) {
            _interaction = interaction;
            _httpContextAccessor = httpContextAccessor;
            _schemeProvider = schemeProvider;
            _clientStore = clientStore;
        }

        public async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl) {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

            if (context?.IdP != null) {
                // This is meant to short circuit the UI and only trigger the one external IdP.
                return new LoginViewModel {
                    EnableLocalLogin = false,
                    ReturnUrl = returnUrl,
                    Username = context?.LoginHint,
                    ExternalProviders = new ExternalProvider[] { new ExternalProvider { AuthenticationScheme = context.IdP } }
                };
            }

            var schemes = await _schemeProvider.GetAllSchemesAsync();

            var providers = schemes.Where(x => x.DisplayName != null)
                                   .Select(x => new ExternalProvider {
                                       DisplayName = x.DisplayName,
                                       AuthenticationScheme = x.Name
                                   }).ToList();

            var allowLocal = true;

            if (context?.ClientId != null) {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.ClientId);

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
                ExternalProviders = providers.ToArray()
            };
        }

        public async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model) {
            var viewModel = await BuildLoginViewModelAsync(model.ReturnUrl);
            viewModel.Username = model.Username;
            viewModel.RememberLogin = model.RememberLogin;

            return viewModel;
        }

        public async Task<RegisterViewModel> BuildRegisterViewModelAsync(string returnUrl) {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

            if (context?.IdP != null) {
                // This is meant to short circuit the UI and only trigger the one external IdP.
                return new RegisterViewModel {
                    ReturnUrl = returnUrl,
                    Username = context?.LoginHint,
                    ExternalProviders = new ExternalProvider[] { new ExternalProvider { AuthenticationScheme = context.IdP } }
                };
            }

            var schemes = await _schemeProvider.GetAllSchemesAsync();

            var providers = schemes.Where(x => x.DisplayName != null)
                                   .Select(x => new ExternalProvider {
                                       DisplayName = x.DisplayName,
                                       AuthenticationScheme = x.Name
                                   }).ToList();

            if (context?.ClientId != null) {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.ClientId);

                if (client != null && client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any()) {
                    providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();

                }
            }

            return new RegisterViewModel {
                ReturnUrl = returnUrl,
                Username = context?.LoginHint,
                ExternalProviders = providers.ToArray()
            };
        }

        public async Task<RegisterViewModel> BuildRegisterViewModelAsync(RegisterRequest model) {
            var viewModel = await BuildRegisterViewModelAsync(model.ReturnUrl);
            viewModel.Username = model.Username;

            return viewModel;
        }

        public async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId) {
            var viewModel = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };
            var user = _httpContextAccessor.HttpContext.User;

            if (user?.Identity.IsAuthenticated != true) {
                // If the user is not authenticated, then just show logged out page.
                viewModel.ShowLogoutPrompt = false;
                return viewModel;
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);

            if (context?.ShowSignoutPrompt == false) {
                // It's safe to automatically sign-out.
                viewModel.ShowLogoutPrompt = false;
                return viewModel;
            }

            // Show the logout prompt. this prevents attacks where the user is automatically signed out by another malicious web page.
            return viewModel;
        }

        public async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId) {
            // Get context information (client name, post logout redirect URI and iframe for federated signout).
            var logout = await _interaction.GetLogoutContextAsync(logoutId);

            var viewModel = new LoggedOutViewModel {
                AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = logout?.ClientId,
                SignOutIframeUrl = logout?.SignOutIFrameUrl,
                LogoutId = logoutId
            };

            var user = _httpContextAccessor.HttpContext.User;

            if (user?.Identity.IsAuthenticated == true) {
                var idp = user.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;

                if (idp != null && idp != IdentityServer4.IdentityServerConstants.LocalIdentityProvider) {
                    var providerSupportsSignout = await _httpContextAccessor.HttpContext.GetSchemeSupportsSignOutAsync(idp);

                    if (providerSupportsSignout) {
                        if (viewModel.LogoutId == null) {
                            // If there's no current logout context, we need to create one this captures necessary info from the current logged in user
                            // before we signout and redirect away to the external IdP for signout.
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
