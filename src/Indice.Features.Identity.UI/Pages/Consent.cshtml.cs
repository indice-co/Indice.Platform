using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the consent screen.</summary>
[Authorize]
[SecurityHeaders]
public class ConsentModel : PageModel
{
    private readonly IStringLocalizer<GrantsPageModel> _localizer;
    private readonly IEventService _eventService;
    private readonly ILogger<GrantsPageModel> _logger;
    private readonly IIdentityServerInteractionService _interaction;

    /// <summary>Creates a new instance of <see cref="LoginPageModel"/> class.</summary>
    /// <param name="logger">A generic interface for logging.</param>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="ConsentModel"/>.</param>
    /// <param name="eventService">Interface for the event service.</param>
    /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ConsentModel(
        ILogger<GrantsPageModel> logger,
        IStringLocalizer<GrantsPageModel> localizer,
        IEventService eventService,
        IIdentityServerInteractionService interaction
    ) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _eventService = eventService;
        _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
    }

    /// <summary>View-model class for the consent page.</summary>
    public ConsentViewModel View { get; set; }

    /// <summary>Model that describes the input of the consent page.</summary>
    [BindProperty]
    public ConsentInputModel Input { get; set; }

    /// <summary>Consent page GET handler.</summary>
    /// <param name="returnUrl">The URL to return to.</param>
    public async Task<IActionResult> OnGetAsync(string returnUrl) {
        View = await BuildViewModelAsync(returnUrl);
        if (View == null) {
            return RedirectToPage("Error");
        }
        Input = new ConsentInputModel {
            ReturnUrl = returnUrl,
        };
        return Page();
    }

    /// <summary>Consent page POST handler.</summary>
    public async Task<IActionResult> OnPost() {
        // Validate return URL is still valid.
        var request = await _interaction.GetAuthorizationContextAsync(Input.ReturnUrl);
        if (request == null) {
            return RedirectToPage("Error");
        }
        ConsentResponse grantedConsent = null;
        // User clicked 'no' - send back the standard 'access_denied' response.
        if (Input?.Button == "no") {
            grantedConsent = new ConsentResponse {
                Error = AuthorizationError.AccessDenied
            };
            // Emit event.
            await _eventService.RaiseAsync(new ConsentDeniedEvent(User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues));
        }
        // User clicked 'yes' - validate the data.
        else if (Input?.Button == "yes") {
            // If the user consented to some scope, build the response model.
            if (Input.ScopesConsented != null && Input.ScopesConsented.Any()) {
                var scopes = Input.ScopesConsented;
                if (ConsentOptions.EnableOfflineAccess == false) {
                    scopes = scopes.Where(x => x != IdentityServerConstants.StandardScopes.OfflineAccess);
                }
                grantedConsent = new ConsentResponse {
                    RememberConsent = Input.RememberConsent,
                    ScopesValuesConsented = scopes.ToArray(),
                    Description = Input.Description
                };

                // Emit event.
                await _eventService.RaiseAsync(new ConsentGrantedEvent(User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues, grantedConsent.ScopesValuesConsented, grantedConsent.RememberConsent));
            } else {
                ModelState.AddModelError("", ConsentOptions.MustChooseOneErrorMessage);
            }
        } else {
            ModelState.AddModelError("", ConsentOptions.InvalidSelectionErrorMessage);
        }
        if (grantedConsent != null) {
            // Communicate outcome of consent back to IdentityServer.
            await _interaction.GrantConsentAsync(request, grantedConsent);
            // Redirect back to authorization endpoint.
            if (request.IsNativeClient() == true) {
                // The client is native, so this change in how to return the response is for better UX for the end user.
                //return this.LoadingPage(Input.ReturnUrl);
            }
            return Redirect(Input.ReturnUrl);
        }
        // We need to redisplay the consent UI.
        View = await BuildViewModelAsync(Input.ReturnUrl, Input);
        return Page();
    }

    private async Task<ConsentViewModel> BuildViewModelAsync(string returnUrl, ConsentInputModel model = null) {
        var request = await _interaction.GetAuthorizationContextAsync(returnUrl);
        if (request != null) {
            return CreateConsentViewModel(model, returnUrl, request);
        } else {
            _logger.LogError("No consent request matching request: {0}", returnUrl);
        }
        return null;
    }

    private ConsentViewModel CreateConsentViewModel(ConsentInputModel model, string returnUrl, AuthorizationRequest request) {
        var viewModel = new ConsentViewModel {
            ClientName = request.Client.ClientName ?? request.Client.ClientId,
            ClientUrl = request.Client.ClientUri,
            ClientLogoUrl = request.Client.LogoUri,
            AllowRememberConsent = request.Client.AllowRememberConsent
        };
        viewModel.IdentityScopes = request
            .ValidatedResources
            .Resources
            .IdentityResources
            .Select(x => CreateScopeViewModel(x, model?.ScopesConsented == null || model.ScopesConsented?.Contains(x.Name) == true))
            .ToArray();
        var resourceIndicators = request
            .Parameters
#if NET6_0
            .GetValues("resource") ?? Enumerable.Empty<string>();
#endif
#if NET7_0_OR_GREATER
            .GetValues(OidcConstants.AuthorizeRequest.Resource) ?? Enumerable.Empty<string>();
#endif
        var apiResources = request
            .ValidatedResources
            .Resources
            .ApiResources
            .Where(x => resourceIndicators.Contains(x.Name));
        var apiScopes = new List<ScopeViewModel>();
        foreach (var parsedScope in request.ValidatedResources.ParsedScopes) {
            var apiScope = request.ValidatedResources.Resources.FindApiScope(parsedScope.ParsedName);
            if (apiScope != null) {
                var scopeViewModel = CreateScopeViewModel(parsedScope, apiScope, model == null || model.ScopesConsented?.Contains(parsedScope.RawValue) == true);
                scopeViewModel.Resources = apiResources
                    .Where(x => x.Scopes.Contains(parsedScope.ParsedName))
                    .Select(x => new ResourceViewModel {
                        Name = x.Name,
                        DisplayName = x.DisplayName ?? x.Name,
                    })
                    .ToArray();
                apiScopes.Add(scopeViewModel);
            }
        }
        if (ConsentOptions.EnableOfflineAccess && request.ValidatedResources.Resources.OfflineAccess) {
            apiScopes.Add(GetOfflineAccessScope(model is null || model.ScopesConsented?.Contains(IdentityServerConstants.StandardScopes.OfflineAccess) == true));
        }
        viewModel.ApiScopes = apiScopes;
        return viewModel;
    }

    private ScopeViewModel CreateScopeViewModel(IdentityResource identity, bool check) => new() {
        Name = identity.Name,
        Value = identity.Name,
        DisplayName = identity.DisplayName ?? identity.Name,
        Description = identity.Description,
        Emphasize = identity.Emphasize,
        Required = identity.Required,
        Checked = check || identity.Required
    };

    private ScopeViewModel CreateScopeViewModel(ParsedScopeValue parsedScopeValue, ApiScope apiScope, bool check) {
        var displayName = apiScope.DisplayName ?? apiScope.Name;
        if (!string.IsNullOrWhiteSpace(parsedScopeValue.ParsedParameter)) {
            displayName += ":" + parsedScopeValue.ParsedParameter;
        }
        return new ScopeViewModel {
            Name = parsedScopeValue.ParsedName,
            Value = parsedScopeValue.RawValue,
            DisplayName = displayName,
            Description = apiScope.Description,
            Emphasize = apiScope.Emphasize,
            Required = apiScope.Required,
            Checked = check || apiScope.Required
        };
    }

    private ScopeViewModel GetOfflineAccessScope(bool check) {
        return new ScopeViewModel {
            Value = IdentityServerConstants.StandardScopes.OfflineAccess,
            DisplayName = ConsentOptions.OfflineAccessDisplayName,
            Description = ConsentOptions.OfflineAccessDescription,
            Emphasize = true,
            Checked = check
        };
    }
}
