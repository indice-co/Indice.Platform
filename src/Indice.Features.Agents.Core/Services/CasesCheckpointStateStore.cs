using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;

using Microsoft.Extensions.Caching.Distributed;
namespace Indice.Features.Agents.Core.Services;

/// <summary>Checkpoint store adapter for cases workflow checkpoints.</summary>
public interface ICasesCheckpointStateStore : ICheckpointStore<JsonElement> {
}

/// <summary>Stores workflow checkpoints in distributed cache as JSON payloads.</summary>
public sealed class DistributedCacheCasesCheckpointStateStore : ICasesCheckpointStateStore
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new() {
        SlidingExpiration = TimeSpan.FromMinutes(15),
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
    };

    private readonly IDistributedCache _cache;

    /// <summary>Creates a new <see cref="DistributedCacheCasesCheckpointStateStore"/>.</summary>
    public DistributedCacheCasesCheckpointStateStore(IDistributedCache cache) {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    private static string Key(string sessionId, string checkpointId) => $"cases:checkpoint:{sessionId}:{checkpointId}";
    private static string IndexKey(string sessionId, string parent) => $"cases:checkpoint:index:{sessionId}:{parent}";

    /// <inheritdoc/>
    public async ValueTask<CheckpointInfo> CreateCheckpointAsync(string sessionId, JsonElement value, CheckpointInfo? parent = null) {
        var info = new CheckpointInfo(sessionId, Guid.NewGuid().ToString("N"));
        var payload = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(Key(sessionId, info.CheckpointId), payload, CacheOptions);

        var parentKey = parent?.CheckpointId ?? "root";
        var indexKey = IndexKey(sessionId, parentKey);
        var existing = await _cache.GetStringAsync(indexKey);
        var list = string.IsNullOrWhiteSpace(existing)
            ? new List<CheckpointInfo>()
            : JsonSerializer.Deserialize<List<CheckpointInfo>>(existing) ?? [];
        list.Add(info);
        await _cache.SetStringAsync(indexKey, JsonSerializer.Serialize(list), CacheOptions);

        return info;
    }

    /// <inheritdoc/>
    public async ValueTask<IEnumerable<CheckpointInfo>> RetrieveIndexAsync(string sessionId, CheckpointInfo? withParent = null) {
        var parentKey = withParent?.CheckpointId ?? "root";
        var existing = await _cache.GetStringAsync(IndexKey(sessionId, parentKey));
        return string.IsNullOrWhiteSpace(existing)
            ? []
            : JsonSerializer.Deserialize<List<CheckpointInfo>>(existing) ?? [];
    }

    /// <inheritdoc/>
    public async ValueTask<JsonElement> RetrieveCheckpointAsync(string sessionId, CheckpointInfo key) {
        var payload = await _cache.GetStringAsync(Key(sessionId, key.CheckpointId));
        if (string.IsNullOrWhiteSpace(payload)) {
            throw new InvalidOperationException($"Checkpoint '{key.CheckpointId}' was not found for session '{sessionId}'.");
        }
        return JsonSerializer.Deserialize<JsonElement>(payload);
    }
}
