namespace Indice.Features.Identity.UI.Models;

/// <summary>Scope model for the consent screen.</summary>
public class ScopeViewModel
{
    /// <summary>Scope name.</summary>
    public string Name { get; set; }
    /// <summary>Scope value.</summary>
    public string Value { get; set; }
    /// <summary>Scope display name.</summary>
    public string DisplayName { get; set; }
    /// <summary>Description. Could be plain text or markdown.</summary>
    public string Description { get; set; }
    /// <summary>Should make the scope stand out.</summary>
    public bool Emphasize { get; set; }
    /// <summary>Can select or not the scope on the consent screen.</summary>
    public bool Required { get; set; }
    /// <summary>Is pre-selected.</summary>
    public bool Checked { get; set; }
    /// <summary>The available resources.</summary>
    public IEnumerable<ResourceViewModel> Resources { get; set; } = new List<ResourceViewModel>();
}
