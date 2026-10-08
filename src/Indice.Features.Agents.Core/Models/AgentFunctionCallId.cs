using System.Diagnostics;

namespace Indice.Features.Agents.Core.Models;


/// <summary>
/// This record represents a unique identifier for an agent session, consisting of a conversation ID and an optional agent name.
/// </summary>
/// <param name="RequestId">The unique identifier for the conversation.</param>
/// <param name="CheckpointId">The optional identifier for the checkpoint.</param>
[DebuggerDisplay("{ToString(),nq}")]
public record AgentFunctionCallId(string RequestId, string? CheckpointId = null)
{
    /// <summary>
    /// The delimiter used to separate the RequestId and CheckpointId in the string representation of the AgentFunctionCallId.
    /// </summary>
    public const char Delimiter = ':';

    /// <summary>
    /// Returns a string representation of the AgentFunctionCallId in the format "RequestId:CheckpointId" if CheckpointId is provided, or just "RequestId" if CheckpointId is null or whitespace.
    /// </summary>
    /// <returns>A string representation of the AgentFunctionCallId.</returns>
    public override string ToString() {
        return RequestId + (string.IsNullOrWhiteSpace(CheckpointId) ? string.Empty : $"{Delimiter}{CheckpointId}");
    }

    /// <summary>
    /// Parses a string representation of an AgentFunctionCallId in the format "RequestId:CheckpointId" or just "RequestId" and returns an AgentFunctionCallId instance.
    /// </summary>
    /// <param name="callId">The string representation of the AgentFunctionCallId to parse.</param>
    /// <returns>An instance of AgentFunctionCallId.</returns>
    /// <exception cref="ArgumentException">Thrown when the callId is null or whitespace.</exception>
    /// <exception cref="FormatException">Thrown when the request ID format is invalid.</exception>
    public static AgentFunctionCallId Parse(string callId) {
        if (string.IsNullOrWhiteSpace(callId)) {
            throw new ArgumentException("Call ID cannot be null or whitespace.", nameof(callId));
        }
        var parts = callId.Split(Delimiter, 2);
        if (string.IsNullOrWhiteSpace(parts[0])) {
            throw new FormatException("Invalid request ID format.");
        }
        var checkpointId = parts.Length > 1 ? parts[1] : null;
        return new AgentFunctionCallId(parts[0], checkpointId);
    }

    /// <summary>
    /// Attempts to parse a string representation of an AgentFunctionCallId in the format "RequestId:CheckpointId" or just "RequestId" and returns a boolean indicating success or failure.
    /// </summary>
    /// <param name="callId">The string representation of the AgentFunctionCallId to parse.</param>
    /// <param name="agentFunctionCallId">When this method returns, contains the parsed AgentFunctionCallId if the parsing succeeded, or null if the parsing failed.</param>
    /// <returns>True if the parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(string callId, out AgentFunctionCallId? agentFunctionCallId) {
        agentFunctionCallId = default;
        if (string.IsNullOrWhiteSpace(callId)) {
            return false;
        }
        try {
            agentFunctionCallId = Parse(callId);
        } catch {
            return false;
        }
        return true;
    }

    /// <summary>Implicit cast from <see cref="AgentFunctionCallId"/> to <seealso cref="string"/>.</summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator string(AgentFunctionCallId value) => value.ToString();

    /// <summary>Explicit cast from <see cref="string"/> to <seealso cref="AgentFunctionCallId"/></summary>
    /// <param name="value">The value to convert.</param>
    public static explicit operator AgentFunctionCallId(string value) => Parse(value);
}
