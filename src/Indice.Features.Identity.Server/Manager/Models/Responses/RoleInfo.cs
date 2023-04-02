namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Models an system role.</summary>
public class RoleInfo
{
    /// <summary>The id of the role.</summary>
    public string Id { get; set; }
    /// <summary>The name of the role.</summary>
    public string Name { get; set; }
    /// <summary>A description for the role.</summary>
    public string Description { get; set; }
}
