using Indice.Features.Agents.Core;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// The outcome of master intent routing for an <c>auto</c> request: either the name of the agent that
/// should handle the request, or an out-of-scope decision when no registered agent fits.
/// </summary>
public class RouteDecision
{
    /// <summary>The name (and DI key) of the chosen agent workflow, or <c>null</c> when the request is out of scope.</summary>
    public string? AgentName { get; set; }

    /// <summary>A short, polite explanation shown to the user when the request is out of scope; otherwise <c>null</c>.</summary>
    public string? Reason { get; set; }

    /// <summary>Whether the request is in scope for any registered agent.</summary>
    public bool IsInScope { get; set; }
    /// <summary>
    /// Indicates whether an error occurred during routing. If true, the AgentName may be null and Reason may contain an error message.
    /// </summary>
    public bool HasError { get; internal set; }

    /// <summary>
    /// Indicates whether a reason message is available. If true, Reason contains a non-empty string explaining the routing decision.
    /// </summary>
    public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

    /// <summary>
    /// Converts the RouteDecision to a ChatResponseUpdate object, which can be used to send a response back to the user.
    /// </summary>
    /// <returns>A ChatResponseUpdate representing the route decision.</returns>
    public ChatResponseUpdate AsChatResponseUpdate(string conversationId) {
        if (HasError) {
            return new ChatResponseUpdate(ChatRole.Assistant, [new ErrorContent(Reason)]) { ConversationId = conversationId };
        } else if (AgentName is null) {
            return new ChatResponseUpdate(ChatRole.Assistant, Reason ?? AgentsConstants.Defaults.OutOfScopeReply) { ConversationId = conversationId };
        }
        return new ChatResponseUpdate(ChatRole.Assistant, Reason ?? AgentsConstants.Defaults.OutOfScopeReply) { ConversationId = conversationId };
    }
}
