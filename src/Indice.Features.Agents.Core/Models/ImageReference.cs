using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// Payload of an <see cref="AgentsConstants.MediaTypes.Image"/> content part: a single image the chat UI renders as a
/// figure. A gallery is several parts, not several URLs in one part.
/// </summary>
/// <remarks>
/// The image travels one of two ways, and the choice matters:
/// <list type="bullet">
/// <item><description>
/// <b>By reference</b> — <see cref="Uri"/> is an <c>http</c>/<c>https</c> or root-relative URL. Costs nothing to stream
/// or persist, and is the right default whenever the image is already hosted somewhere.
/// </description></item>
/// <item><description>
/// <b>By value</b> — <see cref="FromBytes"/> inlines the bytes as a base64 <c>data:</c> URI. Use it when the image has
/// nowhere to be hosted: one the pipeline generated, or an asset embedded in this assembly. Be deliberate about it: the
/// data URI is written into the message's JSON column verbatim, stays there for the life of the conversation, is re-sent
/// on every reload of that conversation, and inflates the payload by roughly a third over the raw bytes.
/// </description></item>
/// </list>
/// <para>
/// The envelope itself is optional: a part typed with a raw <c>image/*</c> media type, whose value is the URL or data
/// URI, renders as the same figure. Reach for this type when the image needs <see cref="Alt"/> or a
/// <see cref="Caption"/> — that is all the envelope buys.
/// </para>
/// </remarks>
public class ImageReference
{
    /// <summary>Absolute location of the image. Only <c>http</c>, <c>https</c>, <c>data:image/</c> and root-relative URLs are rendered; the client drops anything else.</summary>
    /// <remarks>This was named <c>url</c> on the wire until it was renamed; the client still reads that spelling as a fallback so image parts persisted under the old name keep rendering.</remarks>
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    /// <summary>Alternative text announced by screen readers and shown while the image loads. Omit only for purely decorative images.</summary>
    [JsonPropertyName("alt")]
    public string? Alt { get; set; }

    /// <summary>Caption rendered under the image, e.g. the figure number and the document it came from.</summary>
    [JsonPropertyName("caption")]
    public string? Caption { get; set; }

    /// <summary>
    /// Builds a reference carrying <paramref name="bytes"/> inline as a base64 <c>data:</c> URI, for an image with
    /// nowhere to be hosted. See the type's remarks for the cost this commits to on every turn that carries it.
    /// </summary>
    /// <param name="bytes">The raw image bytes.</param>
    /// <param name="mediaType">The image's media type, e.g. <c>image/png</c>.</param>
    /// <param name="alt">Alternative text announced by screen readers.</param>
    /// <param name="caption">Caption rendered under the image.</param>
    public static ImageReference FromBytes(ReadOnlyMemory<byte> bytes, string mediaType, string? alt = null, string? caption = null) => new() {
        // DataContent composes "data:{mediaType};base64,{data}" — the same mechanism the DataContent projection in
        // DexChatResponseExtensions relies on, so the two shapes cannot drift and no base64 is hand-rolled here.
        Uri = new DataContent(bytes, mediaType).Uri,
        Alt = alt,
        Caption = caption
    };
}
