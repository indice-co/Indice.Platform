using Indice.Services;
using Xunit;

namespace Indice.Common.Tests;

public class MagicBytesValidatorTests
{
    private readonly IMagicBytesValidator _validator = new MagicBytesValidator();

    [Theory]
    [InlineData(".jpg",  new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, true)]
    [InlineData(".jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 }, true)]
    [InlineData(".png",  new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, true)]
    [InlineData(".bmp",  new byte[] { 0x42, 0x4D, 0x00, 0x00 }, true)]
    [InlineData(".pdf",  new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }, true)]
    [InlineData(".zip",  new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00 }, true)]
    [InlineData(".docx", new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00 }, true)]
    [InlineData(".xlsx", new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00 }, true)]
    [InlineData(".pptx", new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00 }, true)]
    public async Task IsValidAsync_ValidMagicBytes_ReturnsTrue(string extension, byte[] bytes, bool expected) {
        using var stream = new MemoryStream(bytes);
        var result = await _validator.IsValidAsync(stream, extension);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(".jpg",  new byte[] { 0x89, 0x50, 0x4E, 0x47 })]  // PNG bytes in .jpg file
    [InlineData(".png",  new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 })]  // JPEG bytes in .png file
    [InlineData(".pdf",  new byte[] { 0x50, 0x4B, 0x03, 0x04 })]  // ZIP bytes in .pdf file
    public async Task IsValidAsync_InvalidMagicBytes_ReturnsFalse(string extension, byte[] bytes) {
        using var stream = new MemoryStream(bytes);
        var result = await _validator.IsValidAsync(stream, extension);
        Assert.False(result);
    }

    [Fact]
    public async Task IsValidAsync_UnknownExtension_ReturnsTrue() {
        using var stream = new MemoryStream([ 0x00, 0x01, 0x02, 0x03 ]);
        var result = await _validator.IsValidAsync(stream, ".xyz");
        Assert.True(result);
    }

    [Theory]
    [InlineData(".gif", new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 })]  // GIF87a
    [InlineData(".gif", new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 })]  // GIF89a
    public async Task IsValidAsync_GifVariants_ReturnsTrue(string extension, byte[] bytes) {
        using var stream = new MemoryStream(bytes);
        var result = await _validator.IsValidAsync(stream, extension);
        Assert.True(result);
    }

    [Fact]
    public async Task IsValidAsync_ValidWebP_ReturnsTrue() {
        // RIFF....WEBP structure
        var bytes = new byte[12];
        // RIFF at offset 0
        bytes[0] = 0x52; bytes[1] = 0x49; bytes[2] = 0x46; bytes[3] = 0x46;
        // file size at offset 4 (arbitrary)
        bytes[4] = 0x00; bytes[5] = 0x00; bytes[6] = 0x00; bytes[7] = 0x00;
        // WEBP at offset 8
        bytes[8] = 0x57; bytes[9] = 0x45; bytes[10] = 0x42; bytes[11] = 0x50;

        using var stream = new MemoryStream(bytes);
        var result = await _validator.IsValidAsync(stream, ".webp");
        Assert.True(result);
    }

    [Fact]
    public async Task IsValidAsync_WebPWithRiffButNoWebpMarker_ReturnsFalse() {
        // RIFF header but with "WAVE" marker (audio file) instead of "WEBP"
        var bytes = new byte[12];
        bytes[0] = 0x52; bytes[1] = 0x49; bytes[2] = 0x46; bytes[3] = 0x46;
        bytes[4] = 0x00; bytes[5] = 0x00; bytes[6] = 0x00; bytes[7] = 0x00;
        bytes[8] = 0x57; bytes[9] = 0x41; bytes[10] = 0x56; bytes[11] = 0x45; // "WAVE"

        using var stream = new MemoryStream(bytes);
        var result = await _validator.IsValidAsync(stream, ".webp");
        Assert.False(result);
    }

    [Theory]
    [InlineData("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\"></svg>")]
    public async Task IsValidAsync_ValidSvg_ReturnsTrue(string svgContent) {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgContent));
        var result = await _validator.IsValidAsync(stream, ".svg");
        Assert.True(result);
    }

    [Fact]
    public async Task IsValidAsync_SvgWithBom_ReturnsTrue() {
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var svgBytes = System.Text.Encoding.UTF8.GetBytes("<svg></svg>");
        using var stream = new MemoryStream([ ..bom, ..svgBytes ]);
        var result = await _validator.IsValidAsync(stream, ".svg");
        Assert.True(result);
    }

    [Fact]
    public async Task IsValidAsync_InvalidSvg_ReturnsFalse() {
        // Binary content (JPEG) masquerading as SVG
        using var stream = new MemoryStream([ 0xFF, 0xD8, 0xFF, 0xE0 ]);
        var result = await _validator.IsValidAsync(stream, ".svg");
        Assert.False(result);
    }

    [Fact]
    public async Task IsValidAsync_StreamPositionIsResetAfterValidation() {
        using var stream = new MemoryStream([ 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 ]);
        var initialPosition = stream.Position;
        await _validator.IsValidAsync(stream, ".jpg");
        Assert.Equal(initialPosition, stream.Position);
    }

    [Fact]
    public async Task IsValidAsync_ExtensionWithoutLeadingDot_StillValidates() {
        using var stream = new MemoryStream([ 0xFF, 0xD8, 0xFF, 0xE0 ]);
        var result = await _validator.IsValidAsync(stream, "jpg");
        Assert.True(result);
    }

    [Fact]
    public async Task IsValidAsync_NullOrEmptyExtension_ReturnsTrue() {
        using var stream = new MemoryStream([ 0x00, 0x01 ]);
        var result = await _validator.IsValidAsync(stream, "");
        Assert.True(result);
    }
}
