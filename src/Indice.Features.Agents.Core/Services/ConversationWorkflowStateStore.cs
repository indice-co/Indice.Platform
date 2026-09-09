using Indice.Features.Agents.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Agents.Core.Services;

/// <summary>Stores workflow runtime state in <c>dex.Conversation.WorkflowStateJson</c>.</summary>
public sealed class ConversationWorkflowStateStore : IWorkflowStateStore
{
    private readonly AgentsDbContext _db;

    /// <summary>Creates a new <see cref="ConversationWorkflowStateStore"/>.</summary>
    public ConversationWorkflowStateStore(AgentsDbContext db) {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc/>
    public async Task<string?> GetStateJsonAsync(Guid conversationId, CancellationToken cancellationToken = default) {
        return await _db.Conversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId)
            .Select(c => c.WorkflowStateJson)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SetStateJsonAsync(Guid conversationId, string? stateJson, CancellationToken cancellationToken = default) {
        var conversation = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conversation is null) {
            return;
        }
        conversation.WorkflowStateJson = stateJson;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task ClearStateAsync(Guid conversationId, CancellationToken cancellationToken = default) {
        return SetStateJsonAsync(conversationId, null, cancellationToken);
    }
}
