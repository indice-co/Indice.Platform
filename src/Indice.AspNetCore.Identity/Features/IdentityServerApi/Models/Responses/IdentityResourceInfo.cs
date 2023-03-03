using System.Collections.Generic;

namespace Indice.AspNetCore.Identity.Api.Models;

/// <summary>Models an identity resource for the application.</summary>
public class IdentityResourceInfo
{
    /// <summary>Unique identifier for the identity resource.</summary>
    public int Id { get; set; }
    /// <summary>The name of the resource.</summary>
    public string Name { get; set; }
    /// <summary>The display name of the resource.</summary>
    public string DisplayName { get; set; }
    /// <summary>The description of the resource.</summary>
    public string Description { get; set; }
    /// <summary>Determines whether this resource is enabled or not.</summary>
    public bool Enabled { get; set; }
    /// <summary>Determines whether this resource is required or not.</summary>
    public bool Required { get; set; }
    /// <summary>Determines whether this resource should be displayed emphasized or not.</summary>
    public bool Emphasize { get; set; }
    /// <summary>Determines whether this resource should be displayed in the discovery document or not.</summary>
    public bool ShowInDiscoveryDocument { get; set; }
    /// <summary>Determines whether this resource is editable or not.</summary>
    public bool NonEditable { get; set; }
    /// <summary>List of associated claims that should be included when this resource is requested.</summary>
    public IEnumerable<string> AllowedClaims { get; set; }
}
