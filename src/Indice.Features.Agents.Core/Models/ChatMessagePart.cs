using System.Diagnostics;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Models;

/// <summary>Represents a part of a chat message.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ChatMessagePart
{
    /// <summary>The value of the message part.</summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = null!;
    /// <summary>The content type of the message part (e.g., "text/plain", "text/html").</summary>
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = null!;

    /// <summary>Optional request ID for the message part. 
    /// Used to correlate requests and responses for scenarios such as human in the loop, tool calls, function calls etc.</summary>
    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; } = null!;

    /// <summary>Optional title for the Message Part.</summary>
    /// <remarks>
    /// A label for parts whose payload has nowhere of its own to carry one — an image part uses it as the figure's
    /// caption, which is what lets a bare <c>image/*</c> part be captioned without the
    /// <see cref="AgentsConstants.MediaTypes.Image"/> envelope. Parts whose payload already carries its own heading
    /// (<see cref="Callout.Title"/>, <see cref="Confirmation.Prompt"/>) keep using that; the client ignores this there.
    /// </remarks>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Creates a new <see cref="ChatMessagePart"/> from HTML content.</summary>
    public static ChatMessagePart FromHtml(string html) => new() { Value = html, ContentType = MediaTypeNames.Text.Html };
    /// <summary>Creates a new <see cref="ChatMessagePart"/> from text content.</summary>
    public static ChatMessagePart FromText(string text, string contentType = MediaTypeNames.Text.Markdown, string? name = null, string? requestId = null) => new() { Value = text, ContentType = contentType, Name = name, RequestId = requestId };

    /// <summary>Creates a new <see cref="ChatMessagePart"/> from JSON content.</summary>
    public static ChatMessagePart FromObject<T>(T obj, string contentType = MediaTypeNames.Application.Json, string? name = null, string? requestId = null) where T : notnull {
        var json = JsonSerializer.Serialize(obj, JsonSerializerOptions.Web);
        return new() { Value = json, ContentType = contentType, Name = name, RequestId = requestId };
    }

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
    /// <param name="name">Text rendered under the image as its caption, and used as the image's alt text.</param>
    /// <exception cref="InvalidOperationException">The image data carries no media type.</exception>
    public static ChatMessagePart FromImage(BinaryData imageData, string? name = null) {
        var mediaType = imageData.MediaType ?? throw new InvalidOperationException("Image data must have a media type.");
        return new() { Value = new DataContent(imageData.ToMemory(), mediaType).Uri, ContentType = mediaType, Name = name };
    }

    /// <summary>
    /// Creates a new <see cref="ChatMessagePart"/> from image content, carrying the bytes inline as a base64
    /// </summary>
    /// <param name="imageData">The image bytes.</param>
    /// <param name="contentType">The media type of the image.</param>
    /// <param name="name">Text rendered under the image as its caption, and used as the image's alt text.</param>
    /// <returns>A new <see cref="ChatMessagePart"/> instance.</returns>
    public static ChatMessagePart FromImage(byte[] imageData, string contentType, string? name = null)
        => FromImage(new BinaryData(imageData, mediaType: contentType), name);

    /// <summary>Converts this <see cref="ChatMessagePart"/> into an <see cref="AIContent"/> instance.</summary>
    /// <exception cref="NotSupportedException">The content type cannot be converted to an <see cref="AIContent"/>.</exception>
    public AIContent ToAIContent() => ContentType switch {
        // A pending tool/function invocation surfaced through the dex port. RequestId correlates with the eventual result.
        AgentsConstants.MediaTypes.FunctionCallPort.Request =>
            new FunctionCallContent(RequestId ?? string.Empty, Name ?? string.Empty, DeserializeOrDefault<IDictionary<string, object?>>(Value)),
        // The outcome of a tool/function invocation, correlated back through RequestId.
        AgentsConstants.MediaTypes.FunctionCallPort.Response =>
            new FunctionResultContent(RequestId ?? string.Empty, DeserializeOrDefault<JsonElement>(Value)),
        // A human-in-the-loop confirmation request travels to the model as a function call carrying the Confirmation payload.
        AgentsConstants.MediaTypes.ConfirmationPort.Request =>
            new FunctionCallContent(RequestId ?? string.Empty, Name ?? "confirmation", DeserializeOrDefault<IDictionary<string, object?>>(Value)),
        // The user's confirmation answer is the function's result (payload is a plain string).
        AgentsConstants.MediaTypes.ConfirmationPort.Response =>
            new FunctionResultContent(RequestId ?? string.Empty, Value),
        // Any textual part goes straight in as model-readable text (text/plain, text/markdown, text/html, …).
        var contentType when contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) =>
            new TextContent(Value),
        // Images travel as base64 data: URIs; the DataContent constructor re-validates the shape.
        var contentType when contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) =>
            new DataContent(Value, contentType),
        // Remaining JSON payloads (application/json, application/vnd.indice.*+json) carry raw JSON text — wrap the bytes, preserving the media type.
        var contentType when IsJson(contentType) =>
            new DataContent(Encoding.UTF8.GetBytes(Value), contentType),
        _ => throw new NotSupportedException($"Content type '{ContentType}' cannot be converted to an AIContent."),
    };

    private static bool IsJson(string contentType) =>
        string.Equals(contentType, MediaTypeNames.Application.Json, StringComparison.OrdinalIgnoreCase) ||
        contentType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);

    private static T? DeserializeOrDefault<T>(string json) {
        if (string.IsNullOrWhiteSpace(json)) {
            return default;
        }
        try {
            return JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web);
        } catch (JsonException) {
            return default;
        }
    }

    /// <summary>Returns a debug string representation of the <see cref="ChatMessagePart"/>.</summary>
    public override string ToString() {
        var sb = new StringBuilder($"[{ContentType}] Value={(Value.Length > 20 ? Value.Substring(0, 20) + "..." : Value)}");
        if (!string.IsNullOrWhiteSpace(Name)) {
            sb.Append($", Name={Name}");
        }
        if (!string.IsNullOrWhiteSpace(RequestId)) {
            sb.Append($", RequestId={RequestId}");
        }
        return sb.ToString();
    }
}
