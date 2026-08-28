using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Core.Tests;

/// <summary>
/// Covers <see cref="ChatMessagePart.FromImage"/>, the one API that carries an image <b>by value</b> without going
/// through <c>DataContent</c>'s own projection. It has to produce a complete <c>data:image/…;base64,…</c> URI using the
/// standard (padded) base64 alphabet: the chat UI's <c>isRenderableImageUrl</c> whitelists <c>data:image/</c> and
/// silently drops anything else, so a malformed value here vanishes from the thread with no error to explain it.
/// </summary>
public class ImageContentTests
{
    /// <summary>A PNG magic header plus a byte that base64 maps onto the alphabet's tail, where Base64Url differs.</summary>
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0xFB, 0xFF];

    [Fact]
    public void FromImage_CarriesTheDataUriPrefixAndTheStandardBase64Alphabet() {
        // Regression: this used to emit the Base64Url alphabet ('-'/'_', unpadded) as a bare string with no "data:"
        // prefix, so every part it produced was dropped by the client without a trace.
        var part = ChatMessagePart.FromImage(BinaryData.FromBytes(Png, "image/png"));

        Assert.Equal("image/png", part.ContentType);
        Assert.StartsWith("data:image/png;base64,", part.Value);
        var payload = part.Value[(part.Value.IndexOf(',') + 1)..];
        Assert.DoesNotContain("-", payload);
        Assert.DoesNotContain("_", payload);
        Assert.Equal(Png, Convert.FromBase64String(payload));
    }
}
