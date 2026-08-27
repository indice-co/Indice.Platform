using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Core.Tests;

/// <summary>
/// Covers the two APIs that carry an image <b>by value</b> rather than by URL. Both have to produce a complete
/// <c>data:image/…;base64,…</c> URI using the standard (padded) base64 alphabet: the chat UI's <c>isRenderableImageUrl</c>
/// whitelists <c>data:image/</c> and silently drops anything else, so a malformed value here vanishes from the thread
/// with no error anywhere to explain it.
/// </summary>
public class ImageContentTests
{
    /// <summary>A PNG magic header plus a byte that base64 maps onto the alphabet's tail, where Base64Url differs.</summary>
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0xFB, 0xFF];

    [Fact]
    public void FromBytes_InlinesTheBytesAsABase64DataUri() {
        var image = ImageReference.FromBytes(Png, "image/png", alt: "Dex", caption: "A caption.");

        Assert.StartsWith("data:image/png;base64,", image.Url);
        Assert.Equal(Png, Decode(image.Url));
        Assert.Equal("Dex", image.Alt);
        Assert.Equal("A caption.", image.Caption);
    }

    [Fact]
    public void FromBytes_ProducesAUrlTheClientWillActuallyRender() {
        // Mirrors isRenderableImageUrl in part-contracts.ts: only http(s), data:image/ and root-relative pass.
        var url = ImageReference.FromBytes(Png, "image/png").Url;

        Assert.StartsWith("data:image/", url);
    }

    [Fact]
    public void FromImage_CarriesTheDataUriPrefixAndTheStandardBase64Alphabet() {
        // Regression: this used to emit the Base64Url alphabet ('-'/'_', unpadded) as a bare string with no "data:"
        // prefix, so every part it produced was dropped by the client without a trace.
        var part = ChatMessagePart.FromImage(BinaryData.FromBytes(Png, "image/png"));

        Assert.Equal("image/png", part.ContentType);
        Assert.StartsWith("data:image/png;base64,", part.Value);
        Assert.Equal(Png, Decode(part.Value));
    }

    [Fact]
    public void FromImage_RejectsImageDataWithNoMediaType() {
        // The media type ends up in the data URI and in the part's ContentType, so guessing one would be worse.
        Assert.Throws<InvalidOperationException>(() => ChatMessagePart.FromImage(BinaryData.FromBytes(Png)));
    }

    /// <summary>Decodes a data URI's payload, asserting on the way that it uses the standard base64 alphabet.</summary>
    private static byte[] Decode(string dataUri) {
        var payload = dataUri[(dataUri.IndexOf(',') + 1)..];
        Assert.DoesNotContain("-", payload);
        Assert.DoesNotContain("_", payload);
        return Convert.FromBase64String(payload);
    }
}
