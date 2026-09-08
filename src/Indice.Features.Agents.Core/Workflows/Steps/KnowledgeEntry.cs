using Indice.Features.Agents.Core.Workflows.Events;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Steps;

/// <summary>
/// Entry point of the knowledge pipeline when it runs as an <c>AIAgent</c>: directly, or as a handoff target of the intent router.
/// Speaks the Agent Workflow chat protocol (accumulates incoming <see cref="ChatMessage"/>s, acts on the turn token) and forwards
/// the turn's messages to <see cref="KnowledgeSeed"/>, which converts them into the typed <c>ConversationState</c> the pipeline
/// expects. The two-step entry exists because the chat protocol base only lets a subclass send chat messages and turn tokens.
/// </summary>
public sealed class KnowledgeEntry : ChatProtocolExecutor
{
    /// <summary>The executor id.</summary>
    public const string ExecutorId = "KnowledgeEntry";

    /// <summary>Creates a new <see cref="KnowledgeEntry"/>.</summary>
    public KnowledgeEntry() : base(ExecutorId, new ChatProtocolExecutorOptions { AutoSendTurnToken = false }) { }

    /// <inheritdoc/>
    protected override async ValueTask TakeTurnAsync(List<ChatMessage> messages, IWorkflowContext context, bool? emitEvents, CancellationToken cancellationToken = default) {
        await context.EmitProgressAsync(Id, "Answering from knowledge base", cancellationToken);
        await context.SendMessageAsync(messages, cancellationToken);
    }
}
