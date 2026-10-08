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
/// The envelope is usually unnecessary. A part typed with a raw <c>image/*</c> media type, whose value is the URL or
/// data URI, renders as the same figure, and a <see cref="DataContent"/> carries its caption in
/// <see cref="DataContent.Name"/> — which the projection lifts onto the part. What a bare part cannot do is caption a
/// <b>hosted</b> image: those travel as <see cref="UriContent"/>, which has no name of its own. That is the one case
/// this envelope is still needed for.
/// </para>
/// </remarks>
public class ImageReference
{
    /// <summary>Absolute location of the image. Only <c>http</c>, <c>https</c>, <c>data:image/</c> and root-relative URLs are rendered; the client drops anything else.</summary>
    /// <remarks>This was named <c>url</c> on the wire until it was renamed; the client still reads that spelling as a fallback so image parts persisted under the old name keep rendering.</remarks>
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    /// <summary>Text rendered under the image as its caption, and used as the image's alt text. Omit for a purely decorative image.</summary>
    /// <remarks>
    /// This was two fields, <c>alt</c> and <c>caption</c>, until they were collapsed into one — a producer filled both
    /// with the same sentence, so the client now renders that one string in both roles. It still reads the <c>alt</c>
    /// spelling as a fallback, so image parts persisted under the old shape keep their text.
    /// </remarks>
    [JsonPropertyName("caption")]
    public string? Caption { get; set; }

    /// <summary>
    /// Builds a reference carrying <paramref name="bytes"/> inline as a base64 <c>data:</c> URI, for an image with
    /// nowhere to be hosted. See the type's remarks for the cost this commits to on every turn that carries it.
    /// </summary>
    /// <param name="bytes">The raw image bytes.</param>
    /// <param name="mediaType">The image's media type, e.g. <c>image/png</c>.</param>
    /// <param name="caption">Text rendered under the image as its caption, and used as the image's alt text.</param>
    public static ImageReference FromBytes(ReadOnlyMemory<byte> bytes, string mediaType, string? caption = null) => new() {
        // DataContent composes "data:{mediaType};base64,{data}" — the same mechanism the DataContent projection in
        // DexChatResponseExtensions relies on, so the two shapes cannot drift and no base64 is hand-rolled here.
        Uri = new DataContent(bytes, mediaType).Uri,
        Caption = caption
    };
}
