using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// This record represents a unique identifier for an agent session, consisting of a conversation ID and an optional agent name.
/// </summary>
/// <param name="ConversationId">The unique identifier for the conversation.</param>
/// <param name="AgentName">The optional name of the agent.</param>
public record AgentSessionId (Guid ConversationId, string? AgentName = null)
{
    /// <summary>
    /// Returns a string representation of the AgentSessionId in the format "ConversationId:AgentName" if AgentName is provided, or just "ConversationId" if AgentName is null or whitespace.
    /// </summary>
    /// <returns></returns>
    public string ToKey() {
        return ConversationId + (string.IsNullOrWhiteSpace(AgentName) ? string.Empty : $":{AgentName}");
    }
}
