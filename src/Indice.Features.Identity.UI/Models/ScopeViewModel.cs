namespace Indice.Features.Identity.UI.Models;

/// <summary>Scope view model for the consent page.</summary>
public class ScopeViewModel
{
    /// <summary>Scope name.</summary>
    public string? Value { get; set; }
    /// <summary>Display name.</summary>
    public string? DisplayName { get; set; }
    /// <summary>Description. Could be plain text or markdown.</summary>
    public string? Description { get; set; }
    /// <summary>Should make the scope stand out.</summary>
    public bool Emphasize { get; set; }
    /// <summary>Can unselect the scope on the consent screen or not.</summary>
    public bool Required { get; set; }
    /// <summary>Is preselected.</summary>
    public bool Checked { get; set; }
    /// <summary>Requires Strong customer authentication.</summary>
    public bool RequiresSca { get; set; }
    /// <summary>Extra data for the scope.</summary>
    public object? Metadata { get; set; }
    /// <summary>An id for an optionally selected resource. </summary>
    public string? ResourceId { get; set; }
}
