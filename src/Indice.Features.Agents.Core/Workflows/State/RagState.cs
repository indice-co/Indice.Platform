namespace Indice.Features.Agents.Core.Workflows.State;

/// <summary>
/// Immutable read-only context carried alongside the typed payload through every pipeline edge.
/// Seeded once by <c>DexRunner</c> from the incoming <c>RagRequest</c>; steps forward it untouched
/// via <c>PipelineEnvelope.Next</c> — never mutate.
/// </summary>
public class RagState
{
    /// <summary>The current user question that initiated this pipeline run.</summary>
    public string Question { get; init; } = string.Empty;

    /// <summary>
    /// The chat session this run belongs to. History-aware steps stamp it on their per-run
    /// <c>AgentSession</c> so the <see cref="SessionStoreChatHistoryProvider"/> can load the conversation.
    /// </summary>
    public Guid SessionId { get; init; }
}
