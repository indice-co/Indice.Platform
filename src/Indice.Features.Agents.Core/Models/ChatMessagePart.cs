using System.Buffers.Text;
using System.Text;
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

/// <summary>Represents a part of a chat message.</summary>
public class ChatMessagePart
{
    /// <summary>The value of the message part.</summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = null!;
    /// <summary>The content type of the message part (e.g., "text/plain", "text/html").</summary>
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = null!;

    /// <summary>Creates a new <see cref="ChatMessagePart"/> from HTML content.</summary>
    public static ChatMessagePart FromHtml(string html) => new() { Value = html, ContentType = "text/html" };
    /// <summary>Creates a new <see cref="ChatMessagePart"/> from text content.</summary>
    public static ChatMessagePart FromText(string text, string contentType = "text/markdown") => new() { Value = text, ContentType = contentType };
    /// <summary>Creates a new <see cref="ChatMessagePart"/> from image content.</summary>
    public static ChatMessagePart FromImage(BinaryData imageData) => new() { Value = ToBase64UrlSafeFast(imageData), ContentType = imageData.MediaType ?? throw new InvalidOperationException("Image data must have a media type.") };

    /// <summary>
    /// Alternative faster version using Base64Url (recommended in .NET 5+)
    /// </summary>
    private static string ToBase64UrlSafeFast(BinaryData data) {
        ReadOnlySpan<byte> source = data.ToArray();
        Span<byte> buffer = new byte[Base64.GetMaxEncodedToUtf8Length(source.Length)];

        Base64Url.EncodeToUtf8(source, buffer, out _, out int bytesWritten);

        return Encoding.UTF8.GetString(buffer.Slice(0, bytesWritten));
    }
}
