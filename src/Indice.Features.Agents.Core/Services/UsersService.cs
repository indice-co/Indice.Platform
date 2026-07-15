using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Agents.Core.Services;

/// <inheritdoc/>
public class UsersService : IUsersService
{
    private readonly AgentsDbContext _db;

    /// <summary>Creates a new <see cref="UsersService"/>.</summary>
    public UsersService(AgentsDbContext db) {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task<Profile?> GetAsync(string subjectId, CancellationToken cancellationToken) {
        return await _db.Profiles
            .AsNoTracking()
            .Where(u => u.SubjectId == subjectId)
            .Select(u => new Profile {
                Id = u.Id,
                DisplayName = u.DisplayName,
                Email = u.Email,
                Locale = u.Locale,
                PreferredLanguage = u.PreferredLanguage,
                ResponseStyle = u.ResponseStyle,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                LastSeenAt = u.LastSeenAt,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Profile> UpsertFromClaimsAsync(string subjectId, string? displayName, string? email, string? locale, CancellationToken cancellationToken) {
        var user = await _db.Profiles.FirstOrDefaultAsync(u => u.SubjectId == subjectId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (user is null) {
            user = new DbProfile {
                Id = Guid.NewGuid(),
                SubjectId = subjectId,
                DisplayName = displayName,
                Email = email,
                Locale = locale,
                CreatedAt = now,
                UpdatedAt = now,
                LastSeenAt = now,
                PreferredCategories = []
            };
            _db.Add(user);
        } else {
            // Refresh the cached claim snapshot when it drifts; always record the touch.
            var changed = false;
            if (displayName is not null && user.DisplayName != displayName) { user.DisplayName = displayName; changed = true; }
            if (email is not null && user.Email != email) { user.Email = email; changed = true; }
            if (locale is not null && user.Locale != locale) { user.Locale = locale; changed = true; }
            if (changed) {
                user.UpdatedAt = now;
            }
            user.LastSeenAt = now;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(user);
    }

    /// <inheritdoc/>
    public async Task<Profile> UpdatePreferencesAsync(string subjectId, string? preferredLanguage, string? responseStyle, CancellationToken cancellationToken) {
        var user = await _db.Profiles.FirstAsync(u => u.SubjectId == subjectId, cancellationToken);
        user.PreferredLanguage = preferredLanguage;
        user.ResponseStyle = responseStyle;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(user);
    }

    private static Profile ToDto(DbProfile u) => new() {
        Id = u.Id,
        DisplayName = u.DisplayName,
        Email = u.Email,
        Locale = u.Locale,
        PreferredLanguage = u.PreferredLanguage,
        PreferredCategories = u.PreferredCategories ?? [],
        ResponseStyle = u.ResponseStyle,
        CreatedAt = u.CreatedAt,
        UpdatedAt = u.UpdatedAt,
        LastSeenAt = u.LastSeenAt,
    };
}
