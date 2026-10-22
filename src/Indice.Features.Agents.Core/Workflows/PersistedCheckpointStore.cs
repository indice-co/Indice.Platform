using System.Text.Json;
using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Models;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Agents.Core.Workflows;

/// <summary>
/// A durable <see cref="JsonCheckpointStore"/> backed by <see cref="AgentsDbContext"/> (table <c>dex.Checkpoint</c>).
/// Checkpoint payloads are stored opaquely as JSON; the index is returned ordered by commit time, oldest first,
/// as required by <see cref="CheckpointManager"/> to resolve the latest checkpoint of a run.
/// </summary>
public sealed class PersistedCheckpointStore(AgentsDbContext dbContext) : JsonCheckpointStore
{
    /// <inheritdoc/>
    public override async ValueTask<CheckpointInfo> CreateCheckpointAsync(string sessionId, JsonElement value, CheckpointInfo? parent = null) {
        var checkpointInfo = new CheckpointInfo(sessionId, Guid.CreateVersion7().ToString("N"));
        dbContext.Set<DbCheckpoint>().Add(new DbCheckpoint {
            SessionId = sessionId,
            CheckpointId = checkpointInfo.CheckpointId,
            ParentCheckpointId = parent?.CheckpointId,
            Payload = value.GetRawText(),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
        return checkpointInfo;
    }

    /// <inheritdoc/>
    public override async ValueTask<JsonElement> RetrieveCheckpointAsync(string sessionId, CheckpointInfo key) {
        var row = await dbContext.Set<DbCheckpoint>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.SessionId == sessionId && x.CheckpointId == key.CheckpointId)
            ?? throw new KeyNotFoundException($"Checkpoint '{key.CheckpointId}' was not found for run '{sessionId}'.");
        using var document = JsonDocument.Parse(row.Payload);
        return document.RootElement.Clone();
    }

    /// <inheritdoc/>
    public override async ValueTask<IEnumerable<CheckpointInfo>> RetrieveIndexAsync(string sessionId, CheckpointInfo? withAncestor = null) {
        var query = dbContext.Set<DbCheckpoint>().AsNoTracking().Where(x => x.SessionId == sessionId);
        if (withAncestor is not null) {
            query = query.Where(x => x.ParentCheckpointId == withAncestor.CheckpointId);
        }
        // Materialize then order client-side: index sets are small per run and some providers (e.g. SQLite in tests)
        // cannot translate DateTimeOffset ordering. Oldest-first ordering is a CheckpointManager contract.
        var rows = await query.Select(x => new { x.CheckpointId, x.CreatedAt }).ToListAsync();
        return rows.OrderBy(x => x.CreatedAt).Select(x => new CheckpointInfo(sessionId, x.CheckpointId)).ToList();
    }

    /// <summary>Finds the session (conversation and owning agent) a checkpoint of the given conversation was committed under.</summary>
    /// <remarks>
    /// Session ids are <c>{ConversationId}:{AgentName}</c>; filtering on the conversation prefix lets the database seek the
    /// <c>(SessionId, CheckpointId)</c> primary key, so the cost is bounded by the conversation rather than the whole store.
    /// </remarks>
    /// <param name="conversationId">The conversation the checkpoint must belong to.</param>
    /// <param name="checkpointId">The checkpoint identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The owning <see cref="AgentSessionId"/>, or <see langword="null"/> when the checkpoint is unknown in this conversation.</returns>
    public async ValueTask<AgentSessionId?> FindSessionByCheckpointIdAsync(Guid conversationId, string checkpointId, CancellationToken cancellationToken = default) {
        if (conversationId == Guid.Empty || string.IsNullOrWhiteSpace(checkpointId)) {
            return null;
        }
        var prefix = $"{new AgentSessionId(conversationId)}:";
        var sessionId = await dbContext.Set<DbCheckpoint>()
            .AsNoTracking()
            .Where(x => x.SessionId.StartsWith(prefix) && x.CheckpointId == checkpointId)
            .Select(x => x.SessionId)
            .FirstOrDefaultAsync(cancellationToken);
        return sessionId is not null && AgentSessionId.TryParse(sessionId, out var agentSessionId) ? agentSessionId : null;
    }
}
