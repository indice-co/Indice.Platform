namespace Indice.Features.Identity.Server.Manager.Models.Requests;

/// <summary>Models a user claim that will be updated on the server.</summary>
public class UpdateUserClaimRequest
{
    /// <summary>The value of the claim.</summary>
    public string ClaimValue { get; set; }
}
