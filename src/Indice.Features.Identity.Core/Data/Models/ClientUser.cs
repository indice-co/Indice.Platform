using IdentityServer4.EntityFramework.Entities;

namespace Indice.Features.Identity.Core.Data.Models;

/// <summary>Describes the relationship between a client and a user.</summary>
public class ClientUser
{
    /// <summary>The id of the user.</summary>
    public string UserId { get; set; } = null!;
    /// <summary>The id of the client.</summary>
    public int ClientId { get; set; }
    /// <summary>The client object associated with the user.</summary>
    public virtual Client? Client { get; set; }
}
