using System.Text.Json.Serialization;

namespace Indice.Features.Agents.Core.Models;

/// <summary>Represents the content of a chat message.</summary>
public class ChatMessageContent 
{
    /// <summary>Creates a new instance of <see cref="ChatMessageContent"/>.</summary>
    public ChatMessageContent() {
            
    }
    /// <summary>Creates a new instance of <see cref="ChatMessageContent"/> with a single part.</summary>
    public ChatMessageContent(string content, string contentType = "text/markdown") {
        AddPart(content, contentType);
    }


    /// <summary>Parts of the message content.</summary>
    [JsonPropertyName("parts")]
    public List<ChatMessagePart> Parts { get; set; } = [];

    /// <summary>
    /// Adds a new part to the message content.
    /// </summary>
    /// <param name="value">The value of the message part.</param>
    /// <param name="contentType">The content type of the message part.</param>
    public void AddPart(string value, string contentType) {
        Parts.Add(ChatMessagePart.FromText(value, contentType));
    }
}
