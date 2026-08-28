using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Models;

/// <summary>Represents a part of a chat message.</summary>
public class ChatMessagePart
{
    /// <summary>The value of the message part.</summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = null!;
    /// <summary>The content type of the message part (e.g., "text/plain", "text/html").</summary>
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = null!;

    /// <summary>Optional title for the Message Part.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Creates a new <see cref="ChatMessagePart"/> from HTML content.</summary>
    public static ChatMessagePart FromHtml(string html) => new() { Value = html, ContentType = "text/html" };
    /// <summary>Creates a new <see cref="ChatMessagePart"/> from text content.</summary>
    public static ChatMessagePart FromText(string text, string contentType = "text/markdown") => new() { Value = text, ContentType = contentType };

    /// <summary>
    /// Creates a new <see cref="ChatMessagePart"/> from image content, carrying the bytes inline as a base64
    /// <c>data:</c> URI.
    /// </summary>
    /// <remarks>
    /// The value has to be a complete <c>data:image/…;base64,…</c> URI using the standard (padded) base64 alphabet: the
    /// client renders it straight into an <c>&lt;img src&gt;</c> and silently drops anything that is not a recognised
    /// image URL. <see cref="DataContent"/> composes exactly that, and is the same mechanism the streaming and
    /// aggregated projections use, so the shapes cannot drift.
    /// </remarks>
    /// <param name="imageData">The image bytes. Its <see cref="BinaryData.MediaType"/> must be set.</param>
    /// <exception cref="InvalidOperationException">The image data carries no media type.</exception>
    public static ChatMessagePart FromImage(BinaryData imageData) {
        var mediaType = imageData.MediaType ?? throw new InvalidOperationException("Image data must have a media type.");
        return new() { Value = new DataContent(imageData.ToMemory(), mediaType).Uri, ContentType = mediaType };
    }
}
