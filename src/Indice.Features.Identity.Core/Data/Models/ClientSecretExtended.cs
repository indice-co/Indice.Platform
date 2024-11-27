using System.Text.Json;
using IdentityServer4.EntityFramework.Entities;

namespace Indice.Features.Identity.Core.Data.Models;

/// <summary>Extends the IdentityServer4 <see cref="IdentityServer4.EntityFramework.Entities.ClientSecret"/> table.</summary>
public class ClientSecretExtended
{
    /// <summary>The id of the client.</summary>
    public int ClientSecretId { get; set; }
    /// <summary>Custom data for the client secret entry.</summary>
    public dynamic? CustomData { get; set; }
    
    /// <summary>Custom data for the client secret entry, in the form of JSON.</summary>
    public string? CustomDataJson {
        get => CustomData != null ? JsonSerializer.Serialize(CustomData) : null;
        set => CustomData = value != null ? JsonSerializer.Deserialize<dynamic>(value) : null;
    }
    
    /// <summary>The client object associated with the user.</summary>
    public virtual ClientSecret? ClientSecret { get; set; }
}
