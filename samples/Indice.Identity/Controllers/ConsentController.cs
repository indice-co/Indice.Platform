using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity.Extensions;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Indice.Identity.Controllers
{
    /// <summary>
    /// Contains actions to handle user's consent.
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    [SecurityHeaders]
    [Authorize]
    public class ConsentController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IResourceStore _resourceStore;
        private readonly IEventService _events;
        private readonly ILogger<ConsentController> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="ConsentController"/>.
        /// </summary>
        /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
        /// <param name="clientStore">Retrieval of client configuration.</param>
        /// <param name="resourceStore">Resource retrieval.</param>
        /// <param name="events">Interface for the event service.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public ConsentController(IIdentityServerInteractionService interaction, IClientStore clientStore, IResourceStore resourceStore, IEventService events, ILogger<ConsentController> logger) {
            _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
            _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
            _resourceStore = resourceStore ?? throw new ArgumentNullException(nameof(resourceStore));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Displays the consent page.
        /// </summary>
        /// <param name="returnUrl">The URL to navigate after gives consent.</param>
        [HttpGet]
        public async Task<IActionResult> Index(string returnUrl) {
            var viewModel = await BuildViewModelAsync(returnUrl);
            if (viewModel != null) {
                return View(nameof(Index), viewModel);
            }
            return View("Error");
        }

        /// <summary>
        /// Posts user consent information back to the server.
        /// </summary>
        /// <param name="model">The model that contains user's consent info.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ConsentInputModel model) {
            var result = await ProcessConsent(model);
            if (result.IsRedirect) {
                if (await _clientStore.IsPkceClientAsync(result.ClientId)) {
                    // If the client is PKCE then we assume it's native, so this change in how to return the response is for better UX for the end user.
                    return View("Redirect", new RedirectViewModel { RedirectUrl = result.RedirectUri });
                }
                return Redirect(result.RedirectUri);
            }
            if (result.HasValidationError) {
                ModelState.AddModelError(string.Empty, result.ValidationError);
            }
            if (result.ShowView) {
                return View(nameof(Index), result.ViewModel);
            }
            return View("Error");
        }

        private async Task<ProcessConsentResult> ProcessConsent(ConsentInputModel model) {
            var result = new ProcessConsentResult();
            // Validate return URL is still valid.
            var request = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
            if (request == null) {
                return result;
            }
            ConsentResponse grantedConsent = null;
            // User clicked 'no' - send back the standard 'access_denied' response.
            if (model?.Button == "no") {
                grantedConsent = ConsentResponse.Denied;
                // Emit event.
                await _events.RaiseAsync(new ConsentDeniedEvent(User.GetSubjectId(), request.ClientId, request.ScopesRequested));
            }
            // User clicked 'yes' - validate the data.
            else if (model?.Button == "yes") {
                // if the user consented to some scope, build the response model.
                if (model.ScopesConsented != null && model.ScopesConsented.Any()) {
                    var scopes = model.ScopesConsented;
                    if (ConsentOptions.EnableOfflineAccess == false) {
                        scopes = scopes.Where(x => x != IdentityServerConstants.StandardScopes.OfflineAccess);
                    }
                    grantedConsent = new ConsentResponse {
                        RememberConsent = model.RememberConsent,
                        ScopesConsented = scopes.ToArray()
                    };
                    // Emit event.
                    await _events.RaiseAsync(new ConsentGrantedEvent(User.GetSubjectId(), request.ClientId, request.ScopesRequested, grantedConsent.ScopesConsented, grantedConsent.RememberConsent));
                } else {
                    result.ValidationError = ConsentOptions.MustChooseOneErrorMessage;
                }
            } else {
                result.ValidationError = ConsentOptions.InvalidSelectionErrorMessage;
            }
            if (grantedConsent != null) {
                // Communicate outcome of consent back to IdentityServer.
                await _interaction.GrantConsentAsync(request, grantedConsent);
                // Indicate that's it ok to redirect back to authorization endpoint.
                result.RedirectUri = model.ReturnUrl;
                result.ClientId = request.ClientId;
            } else {
                // We need to redisplay the consent UI.
                result.ViewModel = await BuildViewModelAsync(model.ReturnUrl, model);
            }
            return result;
        }

        private async Task<ConsentViewModel> BuildViewModelAsync(string returnUrl, ConsentInputModel model = null) {
            var request = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (request != null) {
                var client = await _clientStore.FindEnabledClientByIdAsync(request.ClientId);
                if (client != null) {
                    var resources = await _resourceStore.FindEnabledResourcesByScopeAsync(request.ScopesRequested);
                    if (resources != null && (resources.IdentityResources.Any() || resources.ApiResources.Any())) {
                        return CreateConsentViewModel(model, returnUrl, client, resources);
                    } else {
                        _logger.LogError("No scopes matching: {0}.", request.ScopesRequested.Aggregate((x, y) => x + ", " + y));
                    }
                } else {
                    _logger.LogError("Invalid client id: {0}.", request.ClientId);
                }
            } else {
                _logger.LogError("No consent request matching request: {0}.", returnUrl);
            }
            return null;
        }

        private ConsentViewModel CreateConsentViewModel(ConsentInputModel model, string returnUrl, Client client, Resources resources) {
            var viewModel = new ConsentViewModel {
                RememberConsent = model?.RememberConsent ?? true,
                ScopesConsented = model?.ScopesConsented ?? Enumerable.Empty<string>(),
                ScaMethod = model?.ScaMethod,
                ScaCode = model?.ScaCode,
                ReturnUrl = returnUrl,
                ClientName = client.ClientName ?? client.ClientId,
                ClientUrl = client.ClientUri,
                ClientLogoUrl = client.LogoUri,
                AllowRememberConsent = client.AllowRememberConsent
            };
            viewModel.IdentityScopes = resources.IdentityResources.Select(x => CreateScopeViewModel(x, viewModel.ScopesConsented.Contains(x.Name) || model == null)).ToArray();
            viewModel.ResourceScopes = resources.ApiResources.SelectMany(x => x.Scopes).Select(x => CreateScopeViewModel(x, viewModel.ScopesConsented.Contains(x.Name) || model == null));
            if (ConsentOptions.EnableOfflineAccess && resources.OfflineAccess) {
                viewModel.ResourceScopes = viewModel.ResourceScopes.Union(new ScopeViewModel[] {
                    GetOfflineAccessScope(viewModel.ScopesConsented.Contains(IdentityServerConstants.StandardScopes.OfflineAccess) || model == null)
                });
            }
            return viewModel;
        }

        private ScopeViewModel CreateScopeViewModel(IdentityResource identity, bool check) {
            return new ScopeViewModel {
                Name = identity.Name,
                DisplayName = identity.DisplayName,
                Description = identity.Description,
                Emphasize = identity.Emphasize,
                Required = identity.Required,
                Checked = check || identity.Required
            };
        }

        private ScopeViewModel CreateScopeViewModel(Scope scope, bool check) {
            return new ScopeViewModel {
                Name = scope.Name,
                DisplayName = scope.DisplayName,
                Description = scope.Description,
                Emphasize = scope.Emphasize,
                Required = scope.Required,
                Checked = check || scope.Required
            };
        }

        private ScopeViewModel GetOfflineAccessScope(bool check) {
            return new ScopeViewModel {
                Name = IdentityServerConstants.StandardScopes.OfflineAccess,
                DisplayName = ConsentOptions.OfflineAccessDisplayName,
                Description = ConsentOptions.OfflineAccessDescription,
                Emphasize = true,
                Checked = check
            };
        }
    }
}
