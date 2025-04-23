namespace Indice.Features.Identity.UI.Models;

/// <summary>
/// Accept terms view model. Used for the accept terms page. 
/// Will be used to show the terms and conditions when a user has not yet consent to them 
/// or if the consent has expired and needs renewal.
/// </summary>
public class AcceptTermsViewModel
{
    /// <summary>Last update date for the terms and conditions.</summary>
    public DateTimeOffset LastUpdateDate { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>The last date the user accepted the terms and conditions.</summary>
    public DateTimeOffset? LastConsentDate { get; set; }
    /// <summary>The consent was given or not.</summary>
    public bool LastConsent { get; set; }
    /// <summary>An informational banner that can be shown to the user.</summary>
    public AlertModel? Alert { get; set; }
    /// <summary>The URL to redirect the user to after login completes.</summary>
    public string? ReturnUrl { get; set; }
}
