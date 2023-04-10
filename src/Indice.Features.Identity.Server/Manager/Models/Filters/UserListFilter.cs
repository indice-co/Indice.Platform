using System.Collections.Generic;
using Indice.Features.Identity.Server.Manager.Models;

namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Contains filter when querying for users list.</summary>
public class UserListFilter
{
    /// <summary>The type of the claim.</summary>
    public string? ClaimType { get; set; }
    /// <summary>The value of the claim.</summary>
    public string? ClaimValue { get; set; }
    /// <summary>A list of user identifiers to search for.</summary>
    public string[] UserId { get; set; } = Array.Empty<string>();
}
