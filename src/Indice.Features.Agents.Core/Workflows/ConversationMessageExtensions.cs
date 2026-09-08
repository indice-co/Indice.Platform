using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows;

/// <summary>
/// Carries the Dex conversation id on a <see cref="ChatMessage"/> (<see cref="AgentsConstants.MessageProperties.ConversationId"/>),
/// so components that receive messages through the Agent Framework, without access to the run's session or workflow state, can
/// still find the conversation: the pipeline entry executor of a workflow hosted as an agent, and the history provider of a
/// handoff participant whose session is created by the framework.
/// </summary>
public static class ConversationMessageExtensions
{
    /// <summary>Stamps <paramref name="conversationId"/> onto the message's additional properties.</summary>
    public static ChatMessage SetConversationId(this ChatMessage message, Guid conversationId) {
        ArgumentNullException.ThrowIfNull(message);
        message.AdditionalProperties ??= new AdditionalPropertiesDictionary();
        message.AdditionalProperties[AgentsConstants.MessageProperties.ConversationId] = conversationId.ToString();
        return message;
    }

    /// <summary>Reads the conversation id stamped on the message, accepting either a <see cref="Guid"/> or its string form.</summary>
    public static bool TryGetConversationId(this ChatMessage message, out Guid conversationId) {
        conversationId = Guid.Empty;
        if (message?.AdditionalProperties is null || !message.AdditionalProperties.TryGetValue(AgentsConstants.MessageProperties.ConversationId, out var raw)) {
            return false;
        }
        switch (raw) {
            case Guid guid when guid != Guid.Empty:
                conversationId = guid;
                return true;
            case string text when Guid.TryParse(text, out var parsed) && parsed != Guid.Empty:
                conversationId = parsed;
                return true;
            default:
                return false;
        }
    }

    /// <summary>Reads the conversation id from the most recent message that carries one.</summary>
    public static bool TryGetConversationId(this IEnumerable<ChatMessage> messages, out Guid conversationId) {
        conversationId = Guid.Empty;
        if (messages is null) {
            return false;
        }
        foreach (var message in messages.Reverse()) {
            if (message.TryGetConversationId(out conversationId)) {
                return true;
            }
        }
        return false;
    }
}
