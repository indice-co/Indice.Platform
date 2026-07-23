namespace Indice.Features.Agents.Core.Data;

/// <summary>
/// Application-local profile for a user, keyed on the IdP subject claim. The IdP remains the source of
/// truth for identity; this row holds app-specific preferences plus a cached snapshot of a few claim
/// fields (refreshed on each just-in-time touch).
/// </summary>
public class DbProfile
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>The IdP subject claim (<c>sub</c>) this profile belongs to.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Cached display name from the <c>name</c> claim.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Cached email from the <c>email</c> claim.</summary>
    public string? Email { get; set; }

    /// <summary>Cached locale from the <c>locale</c> claim.</summary>
    public string? Locale { get; set; }

    /// <summary>App preference: when set, the composer answers in this language (overrides <see cref="Locale"/>).</summary>
    public string? PreferredLanguage { get; set; }

    /// <summary>App preference: desired answer style (e.g. <c>concise</c> / <c>detailed</c> / <c>formal</c>); feeds composer tone.</summary>
    public string? ResponseStyle { get; set; }

    /// <summary>List of prefered category names. Empty if none sellected</summary>
    public List<string> PreferredCategories { get; set; } = [];

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Timestamp of the last change to profile data (snapshot refresh or preference update).</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Timestamp of the most recent just-in-time touch.</summary>
    public DateTimeOffset LastSeenAt { get; set; }
}
