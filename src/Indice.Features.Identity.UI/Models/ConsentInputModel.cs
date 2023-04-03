namespace Indice.Features.Identity.UI.Models;

/// <summary>Model that describes the input of the consent page.</summary>
public class ConsentInputModel
{
    /// <summary>The button pressed.</summary>
    public string Button { get; set; }
    /// <summary>Description.</summary>
    public string Description { get; set; }
    /// <summary>Remember selection.</summary>
    public bool RememberConsent { get; set; }
    /// <summary>The URL to return.</summary>
    public string ReturnUrl { get; set; }
    /// <summary>Scopes consented.</summary>
    public IEnumerable<string> ScopesConsented { get; set; }
}
