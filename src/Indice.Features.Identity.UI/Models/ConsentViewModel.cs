namespace Indice.Features.Identity.UI.Models;

/// <summary>View-model class for the consent page.</summary>
public class ConsentViewModel
{
    /// <summary>Client name.</summary>
    public string ClientName { get; set; } = string.Empty;
    /// <summary>Client URL.</summary>
    public string? ClientUrl { get; set; }
    /// <summary>Logo URL of the client.</summary>
    public string? ClientLogoUrl { get; set; }
    /// <summary>Determines whether the check box control to allow remember consent will be displayed.</summary>
    public bool AllowRememberConsent { get; set; }
    /// <summary>Identity resources list.</summary>
    public IEnumerable<ScopeViewModel> IdentityScopes { get; set; } = new List<ScopeViewModel>();
    /// <summary>API resources list.</summary>
    public IEnumerable<ScopeViewModel> ApiScopes { get; set; } = new List<ScopeViewModel>();
}
