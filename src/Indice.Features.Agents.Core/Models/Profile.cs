namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// Application-local user profile DTO exposed at the store boundary.
/// Consumed by the Users feature (API) and the RAG personalization provider. The internal
/// <c>SubjectId</c> key is not surfaced.
/// </summary>
public class Profile
{
    /// <summary>Profile identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Cached display name (from the <c>name</c> claim).</summary>
    public string? DisplayName { get; init; }

    /// <summary>Cached email (from the <c>email</c> claim).</summary>
    public string? Email { get; init; }

    /// <summary>Cached locale (from the <c>locale</c> claim).</summary>
    public string? Locale { get; init; }

    /// <summary>Preferred answer language; when set, the composer answers in this language.</summary>
    public string? PreferredLanguage { get; init; }

    /// <summary>Preferred answer style fed to the composer (e.g. <c>concise</c> / <c>detailed</c> / <c>formal</c>).</summary>
    public string? ResponseStyle { get; init; }

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Timestamp of the last change to profile data.</summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Timestamp of the most recent just-in-time touch.</summary>
    public DateTimeOffset LastSeenAt { get; init; }

    /// <summary>
    /// Reasoning-model tokens (prompt + completion) the user consumed in the trailing 7 days.
    /// Computed on read and populated by <c>UsersService</c>
    /// </summary>
    public long ReasoningTokensLast7Days { get; set; }
}
