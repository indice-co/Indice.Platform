namespace Indice.Features.Identity.UI.Areas.Identity.Models;

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
    /// <summary>Strong customer authentication code.</summary>
    public string ScaCode { get; set; }
    /// <summary>Selected Strong customer authentication method.</summary>
    public string ScaMethod { get; set; }
    /// <summary>Scopes consented.</summary>
    public IEnumerable<string> ScopesConsented { get; set; }
}
