using System.Text.Json;
#if NET9_0_OR_GREATER
using Duende.IdentityServer.EntityFramework.Entities;
#else
using IdentityServer4.EntityFramework.Entities;
#endif

namespace Indice.Features.Identity.Core.Data.Models;

/// <summary>Extends the Duende.IdentityServer <see cref="ClientSecret"/> table.</summary>
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
