using System.Text.Json.Serialization;

namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// Payload of an <see cref="AgentsConstants.MediaTypes.Image"/> content part: a single image the chat UI renders as a
/// figure. The image is carried by reference rather than by value — a hosted URL costs nothing to stream or persist,
/// whereas a <c>data:</c> URI is written into the message's JSON column verbatim and stays there for the life of the
/// conversation. A gallery is several parts, not several URLs in one part.
/// </summary>
public class ImageReference
{
    /// <summary>Absolute location of the image. Only <c>http</c>, <c>https</c> and <c>data:image/</c> URLs are rendered; the client drops anything else.</summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>Alternative text announced by screen readers and shown while the image loads. Omit only for purely decorative images.</summary>
    [JsonPropertyName("alt")]
    public string? Alt { get; set; }

    /// <summary>Caption rendered under the image, e.g. the figure number and the document it came from.</summary>
    [JsonPropertyName("caption")]
    public string? Caption { get; set; }
}
