using System.Text.Json;
using Indice.Features.Agents.Core.Models.Cases;
using Microsoft.Extensions.Caching.Distributed;

namespace Indice.Features.Agents.Core.Services;

/// <summary>Persists the replayable Cases state machine state per conversation.</summary>
public interface ICasesReplayStateStore
{
    /// <summary>Gets the replay state for a conversation or <see langword="null"/> when none exists (new or expired).</summary>
    Task<CasesReplayState?> GetAsync(string conversationId, CancellationToken cancellationToken = default);
    /// <summary>Persists the replay state for a conversation.</summary>
    Task SetAsync(CasesReplayState state, CancellationToken cancellationToken = default);
    /// <summary>Removes the replay state for a conversation (terminal phase reached).</summary>
    Task RemoveAsync(string conversationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// An <see cref="ICasesReplayStateStore"/> implementation that stores the replay state as JSON in an
/// <see cref="IDistributedCache"/>, keyed by conversation id. Sliding expiration acts as the session timeout.
/// </summary>
public sealed class DistributedCacheCasesReplayStateStore : ICasesReplayStateStore
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new() {
        SlidingExpiration = TimeSpan.FromMinutes(10),
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
    };

    private readonly IDistributedCache _cache;

    /// <summary>Creates a new <see cref="DistributedCacheCasesReplayStateStore"/>.</summary>
    public DistributedCacheCasesReplayStateStore(IDistributedCache cache) {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    private static string Key(string conversationId) => $"cases-replay:{conversationId}";

    /// <inheritdoc/>
    public async Task<CasesReplayState?> GetAsync(string conversationId, CancellationToken cancellationToken = default) {
        var payload = await _cache.GetStringAsync(Key(conversationId), cancellationToken);
        return payload is null ? null : JsonSerializer.Deserialize<CasesReplayState>(payload);
    }

    /// <inheritdoc/>
    public async Task SetAsync(CasesReplayState state, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(state);
        await _cache.SetStringAsync(Key(state.ConversationId), JsonSerializer.Serialize(state), CacheOptions, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string conversationId, CancellationToken cancellationToken = default) {
        await _cache.RemoveAsync(Key(conversationId), cancellationToken);
    }
}
