//using IdentityServer4;
//using IdentityServer4.Events;
//using IdentityServer4.Extensions;
//using IdentityServer4.Models;
//using IdentityServer4.Services;
//using IdentityServer4.Stores;
//using IdentityServer4.Validation;
//using Indice.AspNetCore.Filters;
//using Indice.AspNetCore.Identity;
//using Indice.AspNetCore.Identity.Data.Models;
//using Indice.AspNetCore.Identity.Extensions;
//using Indice.AspNetCore.Identity.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace Indice.Identity.Controllers;

///// <summary>Contains actions to handle user's consent.</summary>
//[ApiExplorerSettings(IgnoreApi = true)]
//[SecurityHeaders]
//[Authorize]
//[Route("consent")]
//public class ConsentController : Controller
//{
//    private readonly IIdentityServerInteractionService _interaction;
//    private readonly IClientStore _clientStore;
//    private readonly IResourceStore _resourceStore;
//    private readonly IEventService _events;
//    private readonly ILogger<ConsentController> _logger;
//    private readonly TotpServiceFactory _totpServiceFactory;

//    /// <summary>Creates a new instance of <see cref="ConsentController"/>.</summary>
//    /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
//    /// <param name="clientStore">Retrieval of client configuration.</param>
//    /// <param name="resourceStore">Resource retrieval.</param>
//    /// <param name="events">Interface for the event service.</param>
//    /// <param name="logger">Represents a type used to perform logging.</param>
//    /// <param name="totpServiceFactory"></param>
//    public ConsentController(
//        IIdentityServerInteractionService interaction,
//        IClientStore clientStore,
//        IResourceStore resourceStore,
//        IEventService events,
//        ILogger<ConsentController> logger,
//        TotpServiceFactory totpServiceFactory
//    ) {
//        _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
//        _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
//        _resourceStore = resourceStore ?? throw new ArgumentNullException(nameof(resourceStore));
//        _events = events ?? throw new ArgumentNullException(nameof(events));
//        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//        _totpServiceFactory = totpServiceFactory ?? throw new ArgumentNullException(nameof(totpServiceFactory));
//    }

//    /// <summary>Displays the consent page.</summary>
//    /// <param name="returnUrl">The URL to navigate after gives consent.</param>
//    [HttpGet]
//    public async Task<IActionResult> Index(string returnUrl) {
//        var viewModel = await BuildViewModelAsync(returnUrl);
//        if (viewModel != null) {
//            var providers = await _totpServiceFactory.Create<User>().GetProvidersAsync(User);
//            foreach (var provider in providers) {
//                _logger.LogDebug("Provider {Key}:{Value}", provider.Key, provider.Value);
//            }
//            return View(nameof(Index), viewModel);
//        }
//        return View("Error");
//    }

//    /// <summary>Posts user consent information back to the server.</summary>
//    /// <param name="model">The model that contains user's consent info.</param>
//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> Index(ConsentInputModel model) {
//        var result = await ProcessConsent(model);
//        if (result.IsRedirect) {
//            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
//            if (context?.IsNativeClient() == true) {
//                // The client is native, so this change in how to return the response is for better UX for the end user.
//                return this.LoadingPage("Redirect", result.RedirectUri);
//            }
//            return Redirect(result.RedirectUri);
//        }
//        if (result.HasValidationError) {
//            ModelState.AddModelError(string.Empty, result.ValidationError);
//        }
//        if (result.ShowView) {
//            return View(nameof(Index), result.ViewModel);
//        }
//        return View("Error");
//    }

//    private async Task<ProcessConsentResult> ProcessConsent(ConsentInputModel model) {
//        var result = new ProcessConsentResult();
//        // Validate return URL is still valid.
//        var request = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
//        if (request == null) {
//            return result;
//        }
//        ConsentResponse grantedConsent = null;
//        // User clicked 'no' - send back the standard 'access_denied' response.
//        if (model?.Button == "no") {
//            grantedConsent = new ConsentResponse { Error = AuthorizationError.AccessDenied };
//            // Emit event.
//            await _events.RaiseAsync(new ConsentDeniedEvent(User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues));
//        }
//        // User clicked 'yes' - validate the data.
//        else if (model?.Button == "yes") {
//            // If the user consented to some scope, build the response model.
//            if (model.ScopesConsented != null && model.ScopesConsented.Any()) {
//                var scopes = model.ScopesConsented;
//                if (ConsentOptions.EnableOfflineAccess == false) {
//                    scopes = scopes.Where(x => x != IdentityServerConstants.StandardScopes.OfflineAccess);
//                }
//                grantedConsent = new ConsentResponse {
//                    RememberConsent = model.RememberConsent,
//                    ScopesValuesConsented = scopes.ToArray(),
//                    Description = model.Description
//                };
//                // Emit event.
//                await _events.RaiseAsync(new ConsentGrantedEvent(User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues, grantedConsent.ScopesValuesConsented, grantedConsent.RememberConsent));
//            } else {
//                result.ValidationError = ConsentOptions.MustChooseOneErrorMessage;
//            }
//        } else {
//            result.ValidationError = ConsentOptions.InvalidSelectionErrorMessage;
//        }
//        if (grantedConsent != null) {
//            // Communicate outcome of consent back to IdentityServer.
//            await _interaction.GrantConsentAsync(request, grantedConsent);
//            // Indicate that's it ok to redirect back to authorization endpoint.
//            result.RedirectUri = model.ReturnUrl;
//            result.Client = request.Client;
//        } else {
//            // We need to redisplay the consent UI.
//            result.ViewModel = await BuildViewModelAsync(model.ReturnUrl, model);
//        }
//        return result;
//    }

//    private async Task<ConsentViewModel> BuildViewModelAsync(string returnUrl, ConsentInputModel model = null) {
//        var request = await _interaction.GetAuthorizationContextAsync(returnUrl);
//        if (request != null) {
//            return CreateConsentViewModel(model, returnUrl, request);
//        } else {
//            _logger.LogError("No consent request matching request: {0}.", returnUrl);
//        }
//        return null;
//    }

//    private static ConsentViewModel CreateConsentViewModel(ConsentInputModel model, string returnUrl, AuthorizationRequest request) {
//        var viewModel = new ConsentViewModel {
//            RememberConsent = model?.RememberConsent ?? true,
//            ScopesConsented = model?.ScopesConsented ?? Enumerable.Empty<string>(),
//            Description = model?.Description,
//            ReturnUrl = returnUrl,
//            ClientName = request.Client.ClientName ?? request.Client.ClientId,
//            ClientUrl = request.Client.ClientUri,
//            ClientLogoUrl = request.Client.LogoUri,
//            AllowRememberConsent = request.Client.AllowRememberConsent
//        };
//        viewModel.IdentityScopes = request.ValidatedResources.Resources.IdentityResources.Select(x => CreateScopeViewModel(x, viewModel.ScopesConsented.Contains(x.Name) || model == null)).ToArray();
//        var apiScopes = new List<ScopeViewModel>();
//        foreach (var parsedScope in request.ValidatedResources.ParsedScopes) {
//            var apiScope = request.ValidatedResources.Resources.FindApiScope(parsedScope.ParsedName);
//            if (apiScope != null) {
//                var scopeVm = CreateScopeViewModel(parsedScope, apiScope, viewModel.ScopesConsented.Contains(parsedScope.RawValue) || model == null);
//                apiScopes.Add(scopeVm);
//            }
//        }
//        if (ConsentOptions.EnableOfflineAccess && request.ValidatedResources.Resources.OfflineAccess) {
//            apiScopes.Add(GetOfflineAccessScope(viewModel.ScopesConsented.Contains(IdentityServerConstants.StandardScopes.OfflineAccess) || model == null));
//        }
//        viewModel.ApiScopes = apiScopes;
//        return viewModel;
//    }

//    private static ScopeViewModel CreateScopeViewModel(IdentityResource identity, bool check) => new() {
//        Value = identity.Name,
//        DisplayName = identity.DisplayName ?? identity.Name,
//        Description = identity.Description,
//        Emphasize = identity.Emphasize,
//        Required = identity.Required,
//        Checked = check || identity.Required
//    };

//    public static ScopeViewModel CreateScopeViewModel(ParsedScopeValue parsedScopeValue, ApiScope apiScope, bool check) {
//        var displayName = apiScope.DisplayName ?? apiScope.Name;
//        if (!string.IsNullOrWhiteSpace(parsedScopeValue.ParsedParameter)) {
//            displayName += ":" + parsedScopeValue.ParsedParameter;
//        }
//        return new ScopeViewModel {
//            Value = parsedScopeValue.RawValue,
//            DisplayName = displayName,
//            Description = apiScope.Description,
//            Emphasize = apiScope.Emphasize,
//            Required = apiScope.Required,
//            Checked = check || apiScope.Required
//        };
//    }

//    private static ScopeViewModel GetOfflineAccessScope(bool check) => new() {
//        Value = IdentityServerConstants.StandardScopes.OfflineAccess,
//        DisplayName = ConsentOptions.OfflineAccessDisplayName,
//        Description = ConsentOptions.OfflineAccessDescription,
//        Emphasize = true,
//        Checked = check
//    };
//}
