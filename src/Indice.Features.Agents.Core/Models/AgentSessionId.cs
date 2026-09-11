using System.Diagnostics;

namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// This record represents a unique identifier for an agent session, consisting of a conversation ID and an optional agent name.
/// </summary>
/// <param name="ConversationId">The unique identifier for the conversation.</param>
/// <param name="AgentName">The optional name of the agent.</param>
[DebuggerDisplay("{ToString(),nq}")]
public record AgentSessionId (Guid ConversationId, string? AgentName = null)
{
    /// <summary>
    /// Returns a string representation of the AgentSessionId in the format "ConversationId:AgentName" if AgentName is provided, or just "ConversationId" if AgentName is null or whitespace.
    /// </summary>
    /// <returns>A string representation of the AgentSessionId.</returns>
    public override string ToString() {
        return ConversationId + (string.IsNullOrWhiteSpace(AgentName) ? string.Empty : $":{AgentName}");
    }

    /// <summary>
    /// Parses a string representation of an AgentSessionId in the format "ConversationId:AgentName" or just "ConversationId" and returns an AgentSessionId instance.
    /// </summary>
    /// <param name="sessionId">The string representation of the AgentSessionId to parse.</param>
    /// <returns>An instance of AgentSessionId.</returns>
    /// <exception cref="ArgumentException">Thrown when the sessionId is null or whitespace.</exception>
    /// <exception cref="FormatException">Thrown when the conversation ID format is invalid.</exception>
    public static AgentSessionId Parse(string sessionId) {
        if (string.IsNullOrWhiteSpace(sessionId)) {
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));
        }
        var parts = sessionId.Split(':', 2);
        if (!Guid.TryParse(parts[0], out var conversationId)) {
            throw new FormatException("Invalid conversation ID format.");
        }
        var agentName = parts.Length > 1 ? parts[1] : null;
        return new AgentSessionId(conversationId, agentName);
    }

    /// <summary>
    /// Attempts to parse a string representation of an AgentSessionId in the format "ConversationId:AgentName" or just "ConversationId" and returns a boolean indicating success or failure.
    /// </summary>
    /// <param name="sessionId">The string representation of the AgentSessionId to parse.</param>
    /// <param name="agentSessionId">When this method returns, contains the parsed AgentSessionId if the parsing succeeded, or null if the parsing failed.</param>
    /// <returns>True if the parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(string sessionId, out AgentSessionId? agentSessionId) {
        agentSessionId = default;
        if (string.IsNullOrWhiteSpace(sessionId)) {
            return false;
        }
        try {
            agentSessionId = Parse(sessionId);
        } catch {
            return false;
        }
        return true;
    }

    /// <summary>Implicit cast from <see cref="AgentSessionId"/> to <seealso cref="string"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator string(AgentSessionId value) => value.ToString();

    /// <summary>Explicit cast from <see cref="string"/> to <seealso cref="AgentSessionId"/></summary>
    /// <param name="value">The value to convert.</param>
    public static explicit operator AgentSessionId(string value) => Parse(value);
}
