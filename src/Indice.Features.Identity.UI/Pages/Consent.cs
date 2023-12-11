using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core.Scopes;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the consent screen.</summary>
[Authorize]
[IdentityUI(typeof(ConsentModel))]
[SecurityHeaders]
public abstract class BaseConsentModel : BasePageModel
{
    private readonly IStringLocalizer<BaseConsentModel> _localizer;
    
    /// <summary>Creates a new instance of <see cref="BaseConsentModel"/> class.</summary>
    /// <param name="logger">A generic interface for logging.</param>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="BaseConsentModel"/>.</param>
    /// <param name="eventService">Interface for the event service.</param>
    /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseConsentModel(
        ILogger<BaseConsentModel> logger,
        IStringLocalizer<BaseConsentModel> localizer,
        IEventService eventService,
        IIdentityServerInteractionService interaction
    ) {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        EventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        Interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
    }

    /// <summary>Interface for the event service.</summary>
    protected IEventService EventService { get; }
    /// <summary>Provide services be used by the user interface to communicate with IdentityServer.</summary>
    protected IIdentityServerInteractionService Interaction { get; }
    /// <summary>A generic interface for logging.</summary>
    protected ILogger<BaseConsentModel> Logger { get; }

    /// <summary>View-model class for the consent page.</summary>
    public ConsentViewModel View { get; set; } = new ConsentViewModel();

    /// <summary>Model that describes the input of the consent page.</summary>
    [BindProperty]
    public ConsentInputModel Input { get; set; } = new ConsentInputModel();

    /// <summary>Consent page GET handler.</summary>
    /// <param name="returnUrl">The URL to return to.</param>
    public virtual async Task<IActionResult> OnGetAsync(string returnUrl) {
        View = await BuildViewModelAsync(returnUrl);
        if (View is null) {
            return RedirectToPage("/Error");
        }
        Input = new ConsentInputModel {
            ReturnUrl = returnUrl,
        };
        return Page();
    }

    /// <summary>Consent page POST handler.</summary>
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> OnPostAsync() {
        var result = await ProcessConsent(Input);
        if (result.IsRedirect) {
            result.RedirectUri ??= "/";
            var context = await Interaction.GetAuthorizationContextAsync(Input.ReturnUrl);
            if (context?.IsNativeClient() == true) {
                return this.LoadingPage("Redirect", result.RedirectUri);
            }
            return Redirect(result.RedirectUri);
        }
        if (result.HasValidationError) {
            ModelState.AddModelError(string.Empty, result.ValidationError ?? string.Empty);
        }
        if (result.ShowView) {
            View = result.ViewModel;
            return Page();
        }
        return RedirectToPage("/Error");
    }

    private async Task<ProcessConsentResult> ProcessConsent(ConsentInputModel inputModel) {
        if (inputModel is null) {
            throw new ArgumentNullException(nameof(inputModel), "Input model cannot be null.");
        }
        var result = new ProcessConsentResult();
        var request = await Interaction.GetAuthorizationContextAsync(inputModel.ReturnUrl);
        if (request is null) {
            return result;
        }
        ConsentResponse? grantedConsent = null;
        if (inputModel.Button == "no") {
            grantedConsent = new ConsentResponse {
                Error = AuthorizationError.AccessDenied
            };
            await EventService.RaiseAsync(new ConsentDeniedEvent(User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues));
        } else if (inputModel.Button == "yes") {
            if (inputModel.ScopesConsented.Any()) {
                var scopes = inputModel.ScopesConsented;
                if (ConsentOptions.EnableOfflineAccess == false) {
                    scopes = scopes.Where(x => x is not IdentityServerConstants.StandardScopes.OfflineAccess);
                }
                grantedConsent = new ConsentResponse {
                    RememberConsent = inputModel.RememberConsent,
                    ScopesValuesConsented = scopes.ToArray(),
                    Description = inputModel.Description
                };
                await EventService.RaiseAsync(new ConsentGrantedEvent(User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues, grantedConsent.ScopesValuesConsented, grantedConsent.RememberConsent));
            } else {
                result.ValidationError = ConsentOptions.MustChooseOneErrorMessage;
            }
        } else {
            result.ValidationError = ConsentOptions.InvalidSelectionErrorMessage;
        }
        if (grantedConsent is not null) {
            await Interaction.GrantConsentAsync(request, grantedConsent);
            result.RedirectUri = inputModel.ReturnUrl;
            result.Client = request.Client;
        } else {
            result.ViewModel = await BuildViewModelAsync(inputModel.ReturnUrl ?? "/", inputModel);
        }
        return result;
    }

    private async Task<ConsentViewModel> BuildViewModelAsync(string returnUrl, ConsentInputModel? model = null) {
        var request = await Interaction.GetAuthorizationContextAsync(returnUrl);
        if (request is not null) {
            return await CreateConsentViewModel(model, returnUrl, request);
        } else {
            Logger.LogError("No consent request matching request: {ReturnUrl}", returnUrl);
        }
        return new ConsentViewModel();
    }

    private async Task<ConsentViewModel> CreateConsentViewModel(ConsentInputModel? model, string returnUrl, AuthorizationRequest request) {
        var viewModel = new ConsentViewModel {
            RememberConsent = model?.RememberConsent ?? true,
            ScopesConsented = model?.ScopesConsented ?? Enumerable.Empty<string>(),
            Description = model?.Description,
            ReturnUrl = returnUrl,
            ClientName = request.Client.ClientName ?? request.Client.ClientId,
            ClientUrl = request.Client.ClientUri,
            ClientLogoUrl = request.Client.LogoUri,
            AllowRememberConsent = request.Client.AllowRememberConsent
        };
        viewModel.IdentityScopes = request
            .ValidatedResources
            .Resources
            .IdentityResources.Select(x => CreateScopeViewModel(x, viewModel.ScopesConsented.Contains(x.Name) || model is null))
            .ToArray();
        var apiScopes = new List<ScopeViewModel>();
        foreach (var parsedScope in request.ValidatedResources.ParsedScopes) {
            var apiScope = request
                .ValidatedResources
                .Resources
                .FindApiScope(parsedScope.ParsedName);
            if (apiScope is not null) {
                var scopeViewModel = await CreateScopeViewModel(parsedScope, apiScope, viewModel.ScopesConsented.Contains(parsedScope.RawValue) || model is null);
                apiScopes.Add(scopeViewModel);
            }
        }
        if (ConsentOptions.EnableOfflineAccess && request.ValidatedResources.Resources.OfflineAccess) {
            apiScopes.Add(GetOfflineAccessScope(viewModel.ScopesConsented.Contains(IdentityServerConstants.StandardScopes.OfflineAccess) || model is null));
        }
        viewModel.ApiScopes = apiScopes;
        return viewModel;
    }

    private static ScopeViewModel CreateScopeViewModel(IdentityResource identity, bool check) => new ScopeViewModel {
        Value = identity.Name,
        DisplayName = identity.DisplayName ?? identity.Name,
        Description = identity.Description,
        Emphasize = identity.Emphasize,
        Required = identity.Required,
        Checked = check || identity.Required
    };

    /// <summary>
    /// Create a scope view model for api resources.
    /// </summary>
    /// <param name="parsedScopeValue"></param>
    /// <param name="apiScope"></param>
    /// <param name="check"></param>
    /// <returns></returns>
    protected virtual async Task<ScopeViewModel> CreateScopeViewModel(ParsedScopeValue parsedScopeValue, ApiScope apiScope, bool check) {
        var displayName = apiScope.DisplayName ?? apiScope.Name;
        if (!string.IsNullOrWhiteSpace(parsedScopeValue.ParsedParameter)) {
            displayName += ":" + parsedScopeValue.ParsedParameter;
        }

        var model = new ScopeViewModel {
            Value = parsedScopeValue.RawValue,
            DisplayName = displayName,
            Description = apiScope.Description,
            Emphasize = apiScope.Emphasize,
            Required = apiScope.Required,
            Checked = check || apiScope.Required
        };

        if (string.IsNullOrWhiteSpace(parsedScopeValue.ParsedParameter)) {
            return model;
        }

        // Handle dynamic scopes metadata
        var metadataService = ServiceProvider.GetService<IParsedScopeMetadataService>();
        if (metadataService is not null) {
            var metadata = await metadataService.ResolveMetadata(parsedScopeValue, RequestCulture.UICulture);
            model.RequiresSca = metadata.RequiresSca;
            model.DisplayName = string.IsNullOrEmpty(metadata.DisplayName)
                ? model.DisplayName += ":" + parsedScopeValue.ParsedParameter
                : metadata.DisplayName;
            model.Description = metadata.Description;
            model.Metadata = metadata;
        }

        return model;
    }

    private static ScopeViewModel GetOfflineAccessScope(bool check) => new ScopeViewModel {
        Value = IdentityServerConstants.StandardScopes.OfflineAccess,
        DisplayName = ConsentOptions.OfflineAccessDisplayName,
        Description = ConsentOptions.OfflineAccessDescription,
        Emphasize = true,
        Checked = check
    };
}

internal class ConsentModel : BaseConsentModel
{
    public ConsentModel(
        ILogger<ConsentModel> logger,
        IStringLocalizer<ConsentModel> localizer,
        IEventService eventService,
        IIdentityServerInteractionService interaction
    ) : base(logger, localizer, eventService, interaction) { }
}

/// <summary>Represents a consent result object.</summary>
public class ProcessConsentResult
{
    /// <summary>The result is a redirection action.</summary>
    public bool IsRedirect => RedirectUri is not null;
    /// <summary>The redirect URI if available.</summary>
    public string? RedirectUri { get; set; }
    /// <summary>The client.</summary>
    public Client Client { get; set; } = new Client();
    /// <summary>Should show the consent view.</summary>
    public bool ShowView => ViewModel is not null;
    /// <summary>Consent view model</summary>
    public ConsentViewModel ViewModel { get; set; } = new ConsentViewModel();
    /// <summary>checks to see if there is an error.</summary>
    public bool HasValidationError => ValidationError is not null;
    /// <summary>The validation error.</summary>
    public string? ValidationError { get; set; }
}
