using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.State;

/// <summary>
/// Immutable per-run context for the Cases workflow. Seeded once at workflow entry from the
/// incoming chat message; steps treat it as read-only and pass outputs downstream.
/// </summary>
/// <param name="Message">The chat message being processed.</param>
/// <param name="ConversationId">The chat session this run belongs to.</param>
/// <param name="UserIdentifier">The current user's identifier from claims or context provider.</param>
public record CasesWorkflowState(ChatMessage Message, string ConversationId, string UserIdentifier);
