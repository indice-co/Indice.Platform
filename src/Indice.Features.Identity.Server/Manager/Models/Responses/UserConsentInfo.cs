using System;
using System.Collections.Generic;
using static Microsoft.Azure.Amqp.Serialization.SerializableType;

namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Models the </summary>
public class UserConsentInfo
{
    /// <summary>The client id.</summary>
    public string? ClientId { get; set; }
    /// <summary>Consent creation <see cref="DateTime"/>.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Consent expiration <see cref="DateTime"/>.</summary>
    public DateTime? ExpiresAt { get; set; }
    /// <summary>Consent type.</summary>
    public List<string> Types { get; set; } = [];
    /// <summary>Associated scopes.</summary>
    public List<string> Scopes { get; set; } = [];

    public List<UserGrantInfo> Grants { get; set; } = [];


    public void UpdateWith(string type, DateTime createdAt, DateTime? expiresAt, IEnumerable<string> scopes) {
        if (CreatedAt > createdAt) {
            CreatedAt = createdAt;
        }
        if (expiresAt == null) {
            ExpiresAt = null;
        } else if (ExpiresAt < expiresAt) {
            ExpiresAt = expiresAt;
        }
        Scopes = [.. Scopes.Union(scopes)];
        Types = [.. Types.Union([type])];
    }
}
