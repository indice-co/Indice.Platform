using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Persistence boundary for the application-local user profile.
/// exposes <see cref="Profile"/> only. Consumed by the Users feature and the RAG personalization provider.
/// </summary>
public interface IUsersService
{
    /// <summary>Returns the profile for <paramref name="userId"/>, or <c>null</c> when none exists.</summary>
    Task<Profile?> GetAsync(string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the display name for <paramref name="userId"/>, or <c>null</c> when none exists.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The display name, or <c>null</c> if not found.</returns>
    Task<string?> GetDisplayNameAsync(string userId, CancellationToken cancellationToken);
    /// <summary>
    /// Just-in-time provisioning: creates the profile when missing, otherwise refreshes any drifted cached
    /// claim fields; always records the touch in <c>LastSeenAt</c>. Single <c>SaveChanges</c>.
    /// </summary>
    Task<Profile> UpsertFromClaimsAsync(string userId, string? displayName, string? email, string? locale, CancellationToken cancellationToken);

    /// <summary>Sets the app-specific preferences (the row is expected to exist; the service upserts first).</summary>
    Task<Profile> UpdatePreferencesAsync(string userId, string? preferredLanguage, string? responseStyle, List<string>? preferredCategories, CancellationToken cancellationToken);
}
