using System.Text.Json;
using Indice.Features.Agents.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// <see cref="IWorkflowStateStore"/> over the <c>dex.Conversation</c> table: the state lives in
/// <see cref="DbConversation.WorkflowStateJson"/>, so it shares the lifetime of the conversation it grounds
/// and is deleted along with it.
/// </summary>
public sealed class ConversationWorkflowStateStore : IWorkflowStateStore
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    private readonly AgentsDbContext _db;

    /// <summary>Creates a new <see cref="ConversationWorkflowStateStore"/>.</summary>
    public ConversationWorkflowStateStore(AgentsDbContext db) {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc/>
    public async Task<TState?> LoadAsync<TState>(Guid conversationId, CancellationToken cancellationToken = default) where TState : class {
        var json = await _db.Conversations
            .AsNoTracking()
            .Where(conversation => conversation.Id == conversationId)
            .Select(conversation => conversation.WorkflowStateJson)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json)) {
            return null;
        }
        try {
            return JsonSerializer.Deserialize<TState>(json, _serializerOptions);
        } catch (JsonException) {
            // A payload written by another workflow shape must not fault the run; the caller starts over.
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync<TState>(Guid conversationId, TState? state, CancellationToken cancellationToken = default) where TState : class {
        var json = state is null ? null : JsonSerializer.Serialize(state, _serializerOptions);
        await _db.Conversations
            .Where(conversation => conversation.Id == conversationId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(conversation => conversation.WorkflowStateJson, json), cancellationToken);
    }
}
