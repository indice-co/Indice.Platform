using System.ComponentModel.DataAnnotations;
using Indice.Configuration;

namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Models a role that will be created on the server.</summary>
public class CreateRoleRequest
{
    /// <summary>The name of the role.</summary>
    [Required]
    [MaxLength(TextSizePresets.S64)]
    public string? Name { get; set; }
    /// <summary>A description for the role.</summary>
    [MaxLength(TextSizePresets.M512)]
    public string? Description { get; set; }
}
