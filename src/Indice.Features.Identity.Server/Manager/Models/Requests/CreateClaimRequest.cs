namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Models a request to create a claim for an entity (e.x user or client).</summary>
public class CreateClaimRequest
{
    /// <summary>The type of the claim.</summary>
    public string? Type { get; set; }
    /// <summary>The value of the claim.</summary>
    public string? Value { get; set; }
}
