using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Steps;

/// <summary>
/// Second half of the agent-hosted entry (after <see cref="KnowledgeEntry"/>): seeds the typed pipeline with a
/// <see cref="ConversationState"/> built from the turn's messages. The conversation id travels on the user message
/// (see <see cref="ConversationMessageExtensions"/>) because the hosting agent, not this workflow, owns the session.
/// </summary>
public sealed class KnowledgeSeed : Executor<List<ChatMessage>, ConversationState>
{
    /// <summary>The executor id.</summary>
    public const string ExecutorId = "KnowledgeSeed";

    /// <summary>Creates a new <see cref="KnowledgeSeed"/>.</summary>
    public KnowledgeSeed() : base(ExecutorId) { }

    /// <inheritdoc/>
    public override ValueTask<ConversationState> HandleAsync(List<ChatMessage> messages, IWorkflowContext context, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(CreateConversationState(messages));

    /// <summary>
    /// Builds the pipeline seed from a turn's messages. Prefers the last user message that carries a conversation id (the caller's
    /// genuine question; a handoff re-labels other agents' remarks as user messages, and those carry none), then the last user
    /// message. The conversation id comes from that message, else from any stamped message, else it is freshly generated.
    /// </summary>
    /// <exception cref="InvalidOperationException">The turn holds no user message.</exception>
    public static ConversationState CreateConversationState(IReadOnlyList<ChatMessage> messages) {
        ArgumentNullException.ThrowIfNull(messages);
        var message = messages.LastOrDefault(item => item.Role == ChatRole.User && item.TryGetConversationId(out _))
            ?? messages.LastOrDefault(item => item.Role == ChatRole.User)
            ?? throw new InvalidOperationException("The knowledge pipeline needs a user message to answer.");
        var conversationId = message.TryGetConversationId(out var stamped) || messages.TryGetConversationId(out stamped)
            ? stamped
            : Guid.NewGuid();
        return new ConversationState(message, conversationId.ToString());
    }
}
