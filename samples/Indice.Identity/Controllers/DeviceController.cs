using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        private readonly IOptions<IdentityServerOptions> _options;

        /// <summary>
        /// Creates a new instance of <see cref="DeviceController"/>.
        /// </summary>
        /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
        /// <param name="clientStore">Retrieval of client configuration.</param>
        /// <param name="resourceStore">Resource retrieval.</param>
        /// <param name="eventService">Interface for the event service.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        /// <param name="options">he IdentityServerOptions class is the top level container for all configuration settings of IdentityServer.</param>
        public DeviceController(IDeviceFlowInteractionService interaction, IClientStore clientStore, IResourceStore resourceStore, IEventService eventService, ILogger<DeviceController> logger, IOptions<IdentityServerOptions> options) {
            _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
            _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
            _resourceStore = resourceStore ?? throw new ArgumentNullException(nameof(resourceStore));
            _events = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        [HttpGet]
        public async Task<IActionResult> Index() {
            var userCodeParamName = _options.Value.UserInteraction.DeviceVerificationUserCodeParameter;
            string userCode = Request.Query[userCodeParamName];
            if (string.IsNullOrWhiteSpace(userCode)) {
                return View(nameof(UserCodeCapture));
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
                grantedConsent = new ConsentResponse {
                    Error = AuthorizationError.AccessDenied
                };
                // Emit event.
                await _events.RaiseAsync(new ConsentDeniedEvent(User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues));
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
                        ScopesValuesConsented = scopes.ToArray(),
                        Description = model.Description
                    };
                    // Emit event.
                    await _events.RaiseAsync(new ConsentGrantedEvent(User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues, grantedConsent.ScopesValuesConsented, grantedConsent.RememberConsent));
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
                result.Client = request.Client;
            } else {
                // We need to redisplay the consent UI.
                result.ViewModel = await BuildViewModelAsync(model.UserCode, model);
            }
            return result;
        }

        private async Task<DeviceAuthorizationViewModel> BuildViewModelAsync(string userCode, DeviceAuthorizationInputModel model = null) {
            var request = await _interaction.GetAuthorizationContextAsync(userCode);
            if (request != null) {
                return CreateConsentViewModel(userCode, model, request);
            }
            return null;
        }

        private DeviceAuthorizationViewModel CreateConsentViewModel(string userCode, DeviceAuthorizationInputModel model, DeviceFlowAuthorizationRequest request) {
            var viewModel = new DeviceAuthorizationViewModel {
                UserCode = userCode,
                Description = model?.Description,
                RememberConsent = model?.RememberConsent ?? true,
                ScopesConsented = model?.ScopesConsented ?? Enumerable.Empty<string>(),
                ClientName = request.Client.ClientName ?? request.Client.ClientId,
                ClientUrl = request.Client.ClientUri,
                ClientLogoUrl = request.Client.LogoUri,
                AllowRememberConsent = request.Client.AllowRememberConsent
            };
            viewModel.IdentityScopes = request.ValidatedResources.Resources.IdentityResources.Select(x => CreateScopeViewModel(x, viewModel.ScopesConsented.Contains(x.Name) || model == null)).ToArray();
            var apiScopes = new List<ScopeViewModel>();
            foreach (var parsedScope in request.ValidatedResources.ParsedScopes) {
                var apiScope = request.ValidatedResources.Resources.FindApiScope(parsedScope.ParsedName);
                if (apiScope != null) {
                    var scopeViewModel = CreateScopeViewModel(parsedScope, apiScope, viewModel.ScopesConsented.Contains(parsedScope.RawValue) || model == null);
                    apiScopes.Add(scopeViewModel);
                }
            }
            if (ConsentOptions.EnableOfflineAccess && request.ValidatedResources.Resources.OfflineAccess) {
                apiScopes.Add(GetOfflineAccessScope(viewModel.ScopesConsented.Contains(IdentityServerConstants.StandardScopes.OfflineAccess) || model == null));
            }
            viewModel.ApiScopes = apiScopes;
            return viewModel;
        }

        private ScopeViewModel CreateScopeViewModel(IdentityResource identity, bool check) => new ScopeViewModel {
            Value = identity.Name,
            DisplayName = identity.DisplayName ?? identity.Name,
            Description = identity.Description,
            Emphasize = identity.Emphasize,
            Required = identity.Required,
            Checked = check || identity.Required
        };

        public static ScopeViewModel CreateScopeViewModel(ParsedScopeValue parsedScopeValue, ApiScope apiScope, bool check) => new ScopeViewModel {
            Value = parsedScopeValue.RawValue,
            DisplayName = apiScope.DisplayName ?? apiScope.Name,
            Description = apiScope.Description,
            Emphasize = apiScope.Emphasize,
            Required = apiScope.Required,
            Checked = check || apiScope.Required
        };

        private ScopeViewModel GetOfflineAccessScope(bool check) => new ScopeViewModel {
            Value = IdentityServerConstants.StandardScopes.OfflineAccess,
            DisplayName = ConsentOptions.OfflineAccessDisplayName,
            Description = ConsentOptions.OfflineAccessDescription,
            Emphasize = true,
            Checked = check
        };
    }
}
