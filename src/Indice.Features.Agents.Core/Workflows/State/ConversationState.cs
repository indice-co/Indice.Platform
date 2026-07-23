using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.State;

/// <summary>
/// Immutable per-run context stored in the workflow state for downstream steps.
/// Seeded once at workflow entry from the incoming chat message; steps treat it as read-only.
/// </summary>
/// <param name="ConversationId">The chat session this run belongs to. History-aware steps stamp it on their per-run</param>
/// <param name="Message">The chat message being processed.</param>
public record ConversationState(ChatMessage Message, string ConversationId);