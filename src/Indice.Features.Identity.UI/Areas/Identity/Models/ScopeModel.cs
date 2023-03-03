namespace Indice.Features.Identity.UI.Areas.Identity.Models;

/// <summary>Scope model for the consent screen.</summary>
public class ScopeModel
{
    /// <summary>Scope name.</summary>
    public string Value { get; set; }
    /// <summary>Display name.</summary>
    public string DisplayName { get; set; }
    /// <summary>Description. Could be plain text or markdown.</summary>
    public string Description { get; set; }
    /// <summary>Should make the scope stand out.</summary>
    public bool Emphasize { get; set; }
    /// <summary>Can select or not the scope on the consent screen.</summary>
    public bool Required { get; set; }
    /// <summary>Is preselected.</summary>
    public bool Checked { get; set; }
    /// <summary>Specifies if strong customer authentication is required.</summary>
    public bool RequiresSca { get; set; }
    /// <summary>Extra data for the scope.</summary>
    public object Metadata { get; set; }
    /// <summary>An identifier for an optionally selected resource.</summary>
    public string ResourceId { get; set; }
}
