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
internal class MyProfileService : IMyProfileService
{
    /// <summary>Trailing window for the rolling usage figure surfaced on the profile.</summary>
    private const int UsageWindowDays = 7;

    private readonly IUsersService _store;
    private readonly IConversationStore _sessions;
    private readonly TaxonomyOptions _taxonomy;

    /// <summary>Creates a new <see cref="MyProfileService"/>.</summary>
    public MyProfileService(IUsersService store, IConversationStore sessions, IOptions<AgentsOptions> options) {
        _store = store;
        _sessions = sessions;
        _taxonomy = options.Value.Taxonomy;
    }

    /// <inheritdoc/>
    public async Task<Profile> GetMeAsync(ClaimsPrincipal user, CancellationToken cancellationToken) {
        var (subjectId, _, email, locale, displayName) = user.ReadProfile();
        var profile = await _store.UpsertFromClaimsAsync(subjectId, displayName, email, locale, cancellationToken);
        return await WithUsageAsync(profile, subjectId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Profile> UpdateMeAsync(ClaimsPrincipal user, UpdateUserRequest request, CancellationToken cancellationToken) {
        // Config-driven check (can't live in the dependency-free validator): the language must be a configured taxonomy value.
        if (!string.IsNullOrWhiteSpace(request.PreferredLanguage) && !_taxonomy.Languages.Contains(request.PreferredLanguage)) {
            throw new BusinessException($"Unknown language '{request.PreferredLanguage}'.", "TAXONOMY_INVALID", [$"Allowed languages: {string.Join(", ", _taxonomy.Languages)}"]);
        }

        var (subjectId, _, email, locale, displayName) = user.ReadProfile();
        
        await _store.UpsertFromClaimsAsync(subjectId, displayName, email, locale, cancellationToken);
        var profile = await _store.UpdatePreferencesAsync(subjectId, request.PreferredLanguage, request.ResponseStyle, request.PreferredCategories, cancellationToken);
        return await WithUsageAsync(profile, subjectId, cancellationToken);
    }

    /// <summary>Populates the rolling reasoning-token usage on a profile (computed-on-read from the sessions store).</summary>
    private async Task<Profile> WithUsageAsync(Profile profile, string subjectId, CancellationToken cancellationToken) {
        profile.ReasoningTokensLast7Days = await _sessions.GetUsageTokensAsync(
            subjectId, DateTimeOffset.UtcNow.AddDays(-UsageWindowDays), cancellationToken);
        return profile;
    }
}
