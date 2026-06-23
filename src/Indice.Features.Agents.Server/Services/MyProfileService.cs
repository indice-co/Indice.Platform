using System;
using System.Security.Claims;
using Indice.Features.Agents.Core;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Server.Endpoints;
using Indice.Security;
using Indice.Types;
using Microsoft.Extensions.Options;
using static Indice.Features.Agents.Core.AgentsOptions;

namespace Indice.Features.Agents.Server.Services;

/// <inheritdoc/>
internal class MyProfileService
{
    /// <summary>Trailing window for the rolling usage figure surfaced on the profile.</summary>
    private const int UsageWindowDays = 7;

    private readonly IUsersService _store;
    private readonly ISessionsStore _sessions;
    private readonly TaxonomyOptions _taxonomy;

    /// <summary>Creates a new <see cref="UsersService"/>.</summary>
    public MyProfileService(IUsersService store, ISessionsStore sessions, IOptions<AgentsOptions> options) {
        _store = store;
        _sessions = sessions;
        _taxonomy = options.Value.Taxonomy;
    }

    /// <inheritdoc/>
    public async Task<Profile> GetMeAsync(ClaimsPrincipal user, CancellationToken cancellationToken) {
        var (subjectId, name, email, locale) = ReadClaims(user);
        var profile = await _store.UpsertFromClaimsAsync(subjectId, name, email, locale, cancellationToken);
        return await WithUsageAsync(profile, subjectId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Profile> UpdateMeAsync(ClaimsPrincipal user, UpdateUserRequest request, CancellationToken cancellationToken) {
        // Config-driven check (can't live in the dependency-free validator): the language must be a configured taxonomy value.
        if (!string.IsNullOrWhiteSpace(request.PreferredLanguage) && !_taxonomy.Languages.Contains(request.PreferredLanguage)) {
            throw new BusinessException($"Unknown language '{request.PreferredLanguage}'.", "TAXONOMY_INVALID", [$"Allowed languages: {string.Join(", ", _taxonomy.Languages)}"]);
        }
        var (subjectId, name, email, locale) = ReadClaims(user);
        await _store.UpsertFromClaimsAsync(subjectId, name, email, locale, cancellationToken);
        var profile = await _store.UpdatePreferencesAsync(subjectId, request.PreferredLanguage, request.ResponseStyle, cancellationToken);
        return await WithUsageAsync(profile, subjectId, cancellationToken);
    }

    /// <summary>Populates the rolling reasoning-token usage on a profile (computed-on-read from the sessions store).</summary>
    private async Task<Profile> WithUsageAsync(Profile profile, string subjectId, CancellationToken cancellationToken) {
        profile.ReasoningTokensLast7Days = await _sessions.GetUsageTokensAsync(
            subjectId, DateTimeOffset.UtcNow.AddDays(-UsageWindowDays), cancellationToken);
        return profile;
    }

    private static (string subjectId, string? name, string? email, string? locale) ReadClaims(ClaimsPrincipal user)
        => (user.FindSubjectId()!,
            user.FindFirstValue(BasicClaimTypes.Name),
            user.FindFirstValue(BasicClaimTypes.Email),
            user.FindFirstValue(BasicClaimTypes.Locale));
}
