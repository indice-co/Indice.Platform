using Indice.Features.Agents.Core.Services;
using Microsoft.Agents.AI;

namespace Indice.Features.Agents.Core.Workflows;

/// <summary>
/// Read-only <see cref="ChatHistoryProvider"/> over <see cref="IConversationStore"/>. Loads the windowed
/// conversation history (last <c>Session:HistoryWindow</c> turns, oldest-first) for the Dex session stamped
/// on the per-run <see cref="AgentSession"/> via <see cref="SetSessionId"/>, so prior turns reach the model
/// as real chat messages prepended to the request instead of a formatted <c>HISTORY:</c> text block.
/// </summary>
/// <remarks>
/// Writes are deliberately not handled here: <see cref="StoreChatHistoryAsync"/> is a no-op. The pipeline is
/// several internal agents sharing one user-facing conversation, so per-agent request/response pairs are not
/// conversation turns (the composer's request carries <c>CONTEXT:</c> chunks, the classifier's response is a
/// classification). The clean user/assistant turn — with token accounting and title auto-generation — is
/// persisted by <see cref="ChatsService"/> via <see cref="IConversationStore.AppendTurnAsync"/> after the run,
/// which also guarantees the in-flight question is never double-fed through history.
/// </remarks>
public sealed class ConversationStoreChatHistoryProvider : ChatHistoryProvider
{
    private static readonly ProviderSessionState<State> _sessionState = new(
        stateInitializer: _ => new State(),
        stateKey: nameof(ConversationStoreChatHistoryProvider));

    private readonly IConversationStore _store;

    /// <summary>Creates a new <see cref="ConversationStoreChatHistoryProvider"/>.</summary>
    public ConversationStoreChatHistoryProvider(IConversationStore store) {
        _store = store;
    }

    /// <summary>
    /// Stamps the Dex session id onto the per-run <paramref name="agentSession"/>. Session-specific state must
    /// live in the session's state bag (a provider instance serves any session), so this is the only channel
    /// telling the provider which conversation to load.
    /// </summary>
    public static void SetSessionId(AgentSession agentSession, Guid conversationId)
        => _sessionState.SaveState(agentSession, new State { ConversationId = conversationId });

    /// <inheritdoc/>
    protected override async ValueTask<IEnumerable<Microsoft.Extensions.AI.ChatMessage>> ProvideChatHistoryAsync(InvokingContext context, CancellationToken cancellationToken = default) {
        var conversationId = _sessionState.GetOrInitializeState(context.Session).ConversationId;
        if (conversationId == Guid.Empty) {
            return [];
        }
        var history = await _store.GetHistoryAsync(conversationId, cancellationToken);
        return history;
    }

    /// <inheritdoc/>
    protected override ValueTask StoreChatHistoryAsync(InvokedContext context, CancellationToken cancellationToken = default)
        => default;

    private sealed class State
    {
        public Guid ConversationId { get; set; }
    }
}
