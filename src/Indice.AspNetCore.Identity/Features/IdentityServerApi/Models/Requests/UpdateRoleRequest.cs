namespace Indice.AspNetCore.Identity.Api.Models;

/// <summary>Models a role that will be updated on the server.</summary>
public class UpdateRoleRequest
{
    /// <summary>A description for the role.</summary>
    public string Description { get; set; }
}
