using System;
using Json.Schema.Generation;

namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Models a client or API secret that will be created on the server.</summary>
public class CreateSecretRequest
{
    /// <summary>Description of client secret.</summary>
    public string? Description { get; set; }
    /// <summary>The value of client secret.</summary>
    [Required]
    public string Value { get; set; } = null!;
    /// <summary>Optional expiration of client secret.</summary>
    public DateTime? Expiration { get; set; }
}
