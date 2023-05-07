namespace Indice.Features.Identity.UI.Models;

/// <summary>View-model class for the consent page.</summary>
public class ConsentViewModel : ConsentInputModel
{
    /// <summary>Client name.</summary>
    public string? ClientName { get; set; }
    /// <summary>Client site.</summary>
    public string? ClientUrl { get; set; }
    /// <summary>Logo URL of the client.</summary>
    public string? ClientLogoUrl { get; set; }
    /// <summary>Should display the checkbox to allow remember be available.</summary>
    public bool AllowRememberConsent { get; set; }
    /// <summary>Identity scopes.</summary>
    public IEnumerable<ScopeViewModel> IdentityScopes { get; set; } = new List<ScopeViewModel>();
    /// <summary>Resource scopes/</summary>
    public IEnumerable<ScopeViewModel> ApiScopes { get; set; } = new List<ScopeViewModel>();
    /// <summary>Available SCA methods.</summary>
    public Dictionary<string, ScaMethodViewModel> ScaMethods { get; set; } = new Dictionary<string, ScaMethodViewModel>();

    /// <summary>The selected SCA method.</summary>
    public ScaMethodViewModel? SelectedScaMethod {
        get {
            var method = default(ScaMethodViewModel);
            if (ScaMethod is not null && ScaMethods.ContainsKey(ScaMethod) == true) {
                method = ScaMethods[ScaMethod];
            }
            return method;
        }
    }

    /// <summary>Requires strong customer authentication.</summary>
    public bool RequiresSca => ApiScopes?.Where(x => x.RequiresSca).Any() == true;
}
