﻿namespace Indice.Features.Identity.UI.Models;

/// <summary>View model for a given grant.</summary>
public class GrantModel
{
    /// <summary>The client id.</summary>
    public string? ClientId { get; set; }
    /// <summary>The name of the client.</summary>
    public string? ClientName { get; set; }
    /// <summary>The URL to the application site.</summary>
    public string? ClientUrl { get; set; }
    /// <summary>The logo image URL.</summary>
    public string? ClientLogoUrl { get; set; }
    /// <summary>Description.</summary>
    public string? Description { get; set; }
    /// <summary>Date created.</summary>
    public DateTime Created { get; set; }
    /// <summary>If available, the expiration date.</summary>
    public DateTime? Expires { get; set; }
    /// <summary>Any given identity grants (email, profile, address etc).</summary>
    public IEnumerable<string> IdentityGrantNames { get; set; } = Enumerable.Empty<string>();
    /// <summary>Access given to the client in order to access API resources.</summary>
    public IEnumerable<string> ApiGrantNames { get; set; } = Enumerable.Empty<string>();
}
