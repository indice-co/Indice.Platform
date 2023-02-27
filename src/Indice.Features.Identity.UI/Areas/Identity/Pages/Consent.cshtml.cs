using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.UI;
using Indice.Features.Identity.UI.Areas.Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Identity.UI.Areas.Identity.Pages;

/// <summary>Page model for the home/landing screen.</summary>
[Authorize]
[SecurityHeaders]
public class ConsentModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<HomeModel> _localizer;
    private readonly ILogger<HomeModel> _logger;
    private readonly IIdentityServerInteractionService _interaction;

    /// <summary>Creates a new instance of <see cref="LoginModel"/> class.</summary>
    /// <param name="logger">A generic interface for logging.</param>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="ConsentModel"/>.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ConsentModel(
        ILogger<HomeModel> logger,
        IStringLocalizer<HomeModel> localizer,
        IConfiguration configuration,
        IIdentityServerInteractionService interaction
    ) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _configuration = configuration;
        _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
    }

    /// <summary>Manages if the check box control to allow remember consent should be displayed.</summary>
    public bool AllowRememberConsent { get; set; }
    /// <summary>Resource scopes.</summary>
    public IEnumerable<ScopeModel> ApiScopes { get; set; }
    /// <summary>Logo URL of the client.</summary>
    public string ClientLogoUrl { get; set; }
    /// <summary>Client name.</summary>
    public string ClientName { get; set; }
    /// <summary>Client URL.</summary>
    public string ClientUrl { get; set; }
    /// <summary>Identity scopes.</summary>
    public IEnumerable<ScopeModel> IdentityScopes { get; set; }

    /// <summary>Model that describes the input of the consent page.</summary>
    [BindProperty]
    public ConsentInputModel Input { get; set; }

    /// <summary>Requires Strong customer authentication.</summary>
    public bool RequiresSca => ApiScopes?.Where(x => x.RequiresSca).Any() == true;
    /// <summary>Available SCA methods.</summary>
    public Dictionary<string, ScaMethodModel> ScaMethods { get; set; }

    /// <summary>The selected SCA method bound.</summary>
    public ScaMethodModel SelectedScaMethod {
        get {
            var method = default(ScaMethodModel);
            if (ScaMethods?.ContainsKey(Input.ScaMethod) == true) {
                method = ScaMethods[Input.ScaMethod];
            }
            return method;
        }
    }

    /// <summary>Consent page GET handler.</summary>
    public async Task<IActionResult> OnGetAsync(string returnUrl) {
        Input = new ConsentInputModel {
            ReturnUrl = returnUrl
        };
        var isBuilt = await BuildModelAsync();
        if (isBuilt) {
            return Page();
        }
        return RedirectToPage("Error");
    }

    private async Task<bool> BuildModelAsync() {
        var request = await _interaction.GetAuthorizationContextAsync(Input.ReturnUrl);
        if (request is not null) {
            PopulateConsentModel(Input.ReturnUrl, request);
            return true;
        }
        _logger.LogError("No consent request matching request: {ReturnUrl}.", Input.ReturnUrl);
        return false;
    }

    private void PopulateConsentModel(string returnUrl, AuthorizationRequest request) {
        //Input.RememberConsent = model?.RememberConsent ?? true;
        //Input.ScopesConsented = model?.ScopesConsented ?? Enumerable.Empty<string>();
        //Input.Description = model?.Description;
        ClientName = request.Client.ClientName ?? request.Client.ClientId;
        ClientUrl = request.Client.ClientUri;
        ClientLogoUrl = request.Client.LogoUri;
        AllowRememberConsent = request.Client.AllowRememberConsent;
        IdentityScopes = request.ValidatedResources.Resources.IdentityResources.Select(x => CreateScopeModel(x, Input.ScopesConsented.Contains(x.Name))).ToArray();
        var apiScopes = new List<ScopeModel>();
        foreach (var parsedScope in request.ValidatedResources.ParsedScopes) {
            var apiScope = request.ValidatedResources.Resources.FindApiScope(parsedScope.ParsedName);
            if (apiScope != null) {
                var model = CreateScopeModel(parsedScope, apiScope, Input.ScopesConsented.Contains(parsedScope.RawValue));
                apiScopes.Add(model);
            }
        }
        if (ConsentOptions.EnableOfflineAccess && request.ValidatedResources.Resources.OfflineAccess) {
            apiScopes.Add(GetOfflineAccessScope(Input.ScopesConsented.Contains(IdentityServerConstants.StandardScopes.OfflineAccess)));
        }
        ApiScopes = apiScopes;
    }

    private static ScopeModel CreateScopeModel(IdentityResource identity, bool check) => new() {
        Value = identity.Name,
        DisplayName = identity.DisplayName ?? identity.Name,
        Description = identity.Description,
        Emphasize = identity.Emphasize,
        Required = identity.Required,
        Checked = check || identity.Required
    };

    private static ScopeModel CreateScopeModel(ParsedScopeValue parsedScopeValue, ApiScope apiScope, bool check) {
        var displayName = apiScope.DisplayName ?? apiScope.Name;
        if (!string.IsNullOrWhiteSpace(parsedScopeValue.ParsedParameter)) {
            displayName += ":" + parsedScopeValue.ParsedParameter;
        }
        return new ScopeModel {
            Value = parsedScopeValue.RawValue,
            DisplayName = displayName,
            Description = apiScope.Description,
            Emphasize = apiScope.Emphasize,
            Required = apiScope.Required,
            Checked = check || apiScope.Required
        };
    }

    private static ScopeModel GetOfflineAccessScope(bool check) => new ScopeModel {
        Value = IdentityServerConstants.StandardScopes.OfflineAccess,
        DisplayName = ConsentOptions.OfflineAccessDisplayName,
        Description = ConsentOptions.OfflineAccessDescription,
        Emphasize = true,
        Checked = check
    };
}
