namespace Indice.Features.Agents.Server.Endpoints;

/// <summary>Update the caller's app-specific preferences.</summary>
public class UpdateUserRequest
{
    /// <summary>Preferred answer language; <c>null</c>/empty clears it. Validated against <c>Taxonomy.Languages</c>.</summary>
    public string? PreferredLanguage { get; init; }

    /// <summary>Preferred answer style; <c>null</c>/empty clears it. One of <c>concise</c> / <c>detailed</c> / <c>formal</c>.</summary>
    public string? ResponseStyle { get; init; }
}
