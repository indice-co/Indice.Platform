using System.Collections.Immutable;
using Indice.Features.Agents.Core.Models;

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

    /// <summary>Conversation history (oldest-first) providing context for follow-up questions and multi-turn interactions.</summary>
    public ImmutableList<ChatMessage> History { get; init; } = ImmutableList<ChatMessage>.Empty;
}
