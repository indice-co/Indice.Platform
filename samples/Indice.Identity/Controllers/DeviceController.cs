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
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Indice.Identity.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize]
    [SecurityHeaders]
    public class DeviceController : Controller
    {
        private readonly IDeviceFlowInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IResourceStore _resourceStore;
        private readonly IEventService _events;
        private readonly ILogger<DeviceController> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="DeviceController"/>.
        /// </summary>
        /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
        /// <param name="clientStore">Retrieval of client configuration.</param>
        /// <param name="resourceStore">Resource retrieval.</param>
        /// <param name="eventService">Interface for the event service.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public DeviceController(IDeviceFlowInteractionService interaction, IClientStore clientStore, IResourceStore resourceStore, IEventService eventService, ILogger<DeviceController> logger) {
            _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
            _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
            _resourceStore = resourceStore ?? throw new ArgumentNullException(nameof(resourceStore));
            _events = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery(Name = "user_code")] string userCode) {
            if (string.IsNullOrWhiteSpace(userCode)) {
                return View("UserCodeCapture");
            }
            var viewModel = await BuildViewModelAsync(userCode);
            if (viewModel == null) {
                return View("Error");
            }
            viewModel.ConfirmUserCode = true;
            return View("UserCodeConfirmation", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserCodeCapture(string userCode) {
            var viewModel = await BuildViewModelAsync(userCode);
            if (viewModel == null) {
                return View("Error");
            }
            return View("UserCodeConfirmation", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Callback(DeviceAuthorizationInputModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            var result = await ProcessConsent(model);
            if (result.HasValidationError) {
                return View("Error");
            }
            return View("Success");
        }

        private async Task<ProcessConsentResult> ProcessConsent(DeviceAuthorizationInputModel model) {
            var result = new ProcessConsentResult();
            var request = await _interaction.GetAuthorizationContextAsync(model.UserCode);
            if (request == null) {
                return result;
            }
            ConsentResponse grantedConsent = null;
            // User clicked 'no' - send back the standard 'access_denied' response.
            if (model.Button == "no") {
                grantedConsent = ConsentResponse.Denied;
                // Emit event.
                await _events.RaiseAsync(new ConsentDeniedEvent(User.GetSubjectId(), request.ClientId, request.ScopesRequested));
            }
            // User clicked 'yes' - validate the data.
            else if (model.Button == "yes") {
                // If the user consented to some scope, build the response model.
                if (model.ScopesConsented != null && model.ScopesConsented.Any()) {
                    var scopes = model.ScopesConsented;
                    if (ConsentOptions.EnableOfflineAccess == false) {
                        scopes = scopes.Where(x => x != IdentityServer4.IdentityServerConstants.StandardScopes.OfflineAccess);
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
                await _interaction.HandleRequestAsync(model.UserCode, grantedConsent);
                // Indicate that's it ok to redirect back to authorization endpoint.
                result.RedirectUri = model.ReturnUrl;
                result.ClientId = request.ClientId;
            } else {
                // We need to redisplay the consent UI.
                result.ViewModel = await BuildViewModelAsync(model.UserCode, model);
            }
            return result;
        }

        private async Task<DeviceAuthorizationViewModel> BuildViewModelAsync(string userCode, DeviceAuthorizationInputModel model = null) {
            var request = await _interaction.GetAuthorizationContextAsync(userCode);
            if (request != null) {
                var client = await _clientStore.FindEnabledClientByIdAsync(request.ClientId);
                if (client != null) {
                    var resources = await _resourceStore.FindEnabledResourcesByScopeAsync(request.ScopesRequested);
                    if (resources != null && (resources.IdentityResources.Any() || resources.ApiResources.Any())) {
                        return CreateConsentViewModel(userCode, model, client, resources);
                    } else {
                        _logger.LogError("No scopes matching: {0}.", request.ScopesRequested.Aggregate((x, y) => x + ", " + y));
                    }
                } else {
                    _logger.LogError("Invalid client id: {0}.", request.ClientId);
                }
            }
            return null;
        }

        private DeviceAuthorizationViewModel CreateConsentViewModel(string userCode, DeviceAuthorizationInputModel model, Client client, Resources resources) {
            var viewModel = new DeviceAuthorizationViewModel {
                UserCode = userCode,
                RememberConsent = model?.RememberConsent ?? true,
                ScopesConsented = model?.ScopesConsented ?? Enumerable.Empty<string>(),
                ClientName = client.ClientName ?? client.ClientId,
                ClientUrl = client.ClientUri,
                ClientLogoUrl = client.LogoUri,
                AllowRememberConsent = client.AllowRememberConsent
            };
            viewModel.IdentityScopes = resources.IdentityResources.Select(x => CreateScopeViewModel(x, viewModel.ScopesConsented.Contains(x.Name) || model == null)).ToArray();
            viewModel.ResourceScopes = resources.ApiResources.SelectMany(x => x.Scopes).Select(x => CreateScopeViewModel(x, viewModel.ScopesConsented.Contains(x.Name) || model == null)).ToArray();
            if (ConsentOptions.EnableOfflineAccess && resources.OfflineAccess) {
                viewModel.ResourceScopes = viewModel.ResourceScopes.Union(new[] {
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
