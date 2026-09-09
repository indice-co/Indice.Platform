using System.Text.Json;

namespace Indice.Features.Agents.Core.Models;

/// <summary>Conversation-level metadata persisted in <c>DbConversation.MetadataJson</c>.</summary>
public sealed class ConversationMetadata
{
    /// <summary>Sticky workflow selection for this conversation (<c>knowledge</c> or <c>cases</c>).</summary>
    public string? SelectedWorkflow { get; set; }

    /// <summary>Parses metadata JSON into a model instance, returning an empty object on invalid payloads.</summary>
    public static ConversationMetadata Parse(string? json) {
        if (string.IsNullOrWhiteSpace(json)) {
            return new ConversationMetadata();
        }
        try {
            return JsonSerializer.Deserialize<ConversationMetadata>(json) ?? new ConversationMetadata();
        } catch {
            return new ConversationMetadata();
        }
    }

    /// <summary>Serializes the metadata payload for storage in conversation metadata.</summary>
    public string ToJson() => JsonSerializer.Serialize(this);
}
