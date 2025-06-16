namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Models the </summary>
public class UserClientInfo
{
    /// <summary>The client id.</summary>
    public string? ClientId { get; set; }
    /// <summary>Application name that will be seen on consent screens.</summary>
    public string? ClientName { get; set; }
    /// <summary>Application description.</summary>
    public string? Description { get; set; }
    /// <summary>Application URL that will be seen on consent screens.</summary>
    public string? ClientUri { get; set; }
    /// <summary>Application logo that will be seen on consent screens.</summary>
    public string? LogoUri { get; set; }
    /// <summary>Consent creation <see cref="DateTime"/>.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Consent expiration <see cref="DateTime"/>.</summary>
    public DateTime? ExpiresAt { get; set; }
    /// <summary>Consent type.</summary>
    public List<string> Types { get; set; } = [];
    /// <summary>Associated scopes.</summary>
    public List<string> Scopes { get; set; } = [];
    /// <summary>The issued grants requested by the client and granted by the user.</summary>
    public List<UserGrantInfo> Grants { get; set; } = [];

    /// <summary>
    /// Updates the consent information with the given parameters.
    /// </summary>
    /// <param name="type">The grant type</param>
    /// <param name="createdAt">The grant creation time</param>
    /// <param name="expiresAt">The grant expiration time</param>
    /// <param name="scopes">The scopes requested</param>
    public void UpdateWith(string type, DateTime createdAt, DateTime? expiresAt, IEnumerable<string>? scopes) {
        if (CreatedAt == default || CreatedAt > createdAt) {
            CreatedAt = createdAt;
        }
        if (expiresAt == null) {
            ExpiresAt = null;
        } else if (ExpiresAt < expiresAt) {
            ExpiresAt = expiresAt;
        }
        Scopes = [.. Scopes.Union(scopes ?? [])];
        Types = [.. Types.Union([type])];
    }
}
