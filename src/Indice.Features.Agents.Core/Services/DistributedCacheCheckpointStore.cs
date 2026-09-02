using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using Microsoft.Extensions.Caching.Distributed;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// An <see cref="ICheckpointStore{TStoreObject}"/> implementation that persists workflow checkpoints
/// as JSON in an <see cref="IDistributedCache"/>, keyed by run (conversation) id.
/// Used by the Cases workflow to halt for user input and resume on the next chat message.
/// </summary>
public sealed class DistributedCacheCheckpointStore : JsonCheckpointStore
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new() {
        SlidingExpiration = TimeSpan.FromHours(1)
    };

    private readonly IDistributedCache _cache;

    /// <summary>Creates a new <see cref="DistributedCacheCheckpointStore"/>.</summary>
    public DistributedCacheCheckpointStore(IDistributedCache cache) {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    private static string CheckpointKey(string runId, CheckpointInfo key) => $"wf-checkpoint:{runId}:{key.CheckpointId}";
    private static string IndexKey(string runId) => $"wf-checkpoint-index:{runId}";

    /// <inheritdoc/>
    public override async ValueTask<CheckpointInfo> CreateCheckpointAsync(string runId, JsonElement value, CheckpointInfo? parent = null) {
        var checkpointInfo = new CheckpointInfo(runId, Guid.NewGuid().ToString("N"));
        await _cache.SetStringAsync(CheckpointKey(runId, checkpointInfo), value.GetRawText(), CacheOptions);
        var index = await ReadIndexAsync(runId);
        index.Add(checkpointInfo.CheckpointId);
        await _cache.SetStringAsync(IndexKey(runId), JsonSerializer.Serialize(index), CacheOptions);
        return checkpointInfo;
    }

    /// <inheritdoc/>
    public override async ValueTask<JsonElement> RetrieveCheckpointAsync(string runId, CheckpointInfo key) {
        var payload = await _cache.GetStringAsync(CheckpointKey(runId, key))
            ?? throw new KeyNotFoundException($"Checkpoint '{key.CheckpointId}' not found for run '{runId}'.");
        using var document = JsonDocument.Parse(payload);
        return document.RootElement.Clone();
    }

    /// <inheritdoc/>
    public override async ValueTask<IEnumerable<CheckpointInfo>> RetrieveIndexAsync(string runId, CheckpointInfo? withParent = null) {
        var index = await ReadIndexAsync(runId);
        return index.Select(checkpointId => new CheckpointInfo(runId, checkpointId)).ToList();
    }

    private async Task<List<string>> ReadIndexAsync(string runId) {
        var raw = await _cache.GetStringAsync(IndexKey(runId));
        return raw is null ? [] : JsonSerializer.Deserialize<List<string>>(raw) ?? [];
    }
}
