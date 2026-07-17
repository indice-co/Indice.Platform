using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.State;

/// <summary>
/// Immutable read-only context carried alongside the typed payload through every pipeline edge.
/// Seeded once by <c>DexRunner</c> from the incoming <c>RagRequest</c>; steps forward it untouched
/// via <c>PipelineEnvelope.Next</c> — never mutate.
/// </summary>
/// <param name="ConversationId">The chat session this run belongs to. History-aware steps stamp it on their per-run</param>
/// <param name="Message">The chat message being processed.</param>
public record ConversationState(ChatMessage Message, string ConversationId);