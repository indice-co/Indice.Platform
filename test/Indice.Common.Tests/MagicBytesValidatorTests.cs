using System.Text;
using Indice.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Indice.Common.Tests;

public static class MagicBytesTestExtensions
{
    public static MemoryStream ToStream(this byte[] bytes) => new(bytes);
}

public class MagicBytesValidatorTests
{
    private readonly IMagicBytesValidator _validator;

    public MagicBytesValidatorTests() {
        _validator = new MagicBytesValidator(NullLogger<MagicBytesValidator>.Instance);
    }

    [Theory]
    [InlineData(".jpg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, true)]
    [InlineData(".jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 }, true)]
    [InlineData(".png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, true)]
    [InlineData(".bmp", new byte[] { 0x42, 0x4D, 0x00, 0x00 }, true)]
    [InlineData(".pdf", new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }, true)]
    [InlineData(".zip", new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00 }, true)]
    [InlineData(".docx", new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00 }, true)]
    [InlineData(".xlsx", new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00 }, true)]
    [InlineData(".pptx", new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00 }, true)]
    public async Task IsValid_ValidMagicBytes_ReturnsValid(string extension, byte[] bytes, bool expected) {
        using var stream = bytes.ToStream();
        Assert.Equal(expected, (await _validator.IsValid(stream, extension)).IsValid);
    }

    [Theory]
    [InlineData(".jpg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, true)]
    [InlineData(".jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 }, true)]
    [InlineData(".png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, true)]
    [InlineData(".bmp", new byte[] { 0x42, 0x4D, 0x00, 0x00 }, true)]
    [InlineData(".pdf", new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }, true)]
    [InlineData(".zip", new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00 }, true)]
    [InlineData(".docx", new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00 }, true)]
    [InlineData(".xlsx", new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00 }, true)]
    [InlineData(".pptx", new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00 }, true)]
    public async Task IsValidAsync_ValidMagicBytes_ReturnsValid(string extension, byte[] bytes, bool expected) {
        await using var stream = bytes.ToStream();
        Assert.Equal(expected, (await _validator.IsValid(stream, extension)).IsValid);
    }

    [Theory]
    [InlineData(".jpeg", new byte[] { 0x89, 0x50, 0x4E, 0x47 })]
    [InlineData(".png", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 })]
    [InlineData(".pdf", new byte[] { 0x50, 0x4B, 0x03, 0x04 })]
    public async Task IsValid_InvalidMagicBytes_ReturnsFailure(string extension, byte[] bytes) {
        using var stream = bytes.ToStream();
        Assert.False((await _validator.IsValid(stream, extension)).IsValid);
    }

    [Theory]
    [InlineData(".jpeg", new byte[] { 0x89, 0x50, 0x4E, 0x47 })]
    [InlineData(".png", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 })]
    [InlineData(".pdf", new byte[] { 0x50, 0x4B, 0x03, 0x04 })]
    public async Task IsValidAsync_InvalidMagicBytes_ReturnsFailure(string extension, byte[] bytes) {
        await using var stream = bytes.ToStream();
        Assert.False((await _validator.IsValid(stream, extension)).IsValid);
    }

    [Fact]
    public async Task IsValid_UnknownExtension_ReturnsUnknownExtension() {
        using var stream = new byte[] { 0x00, 0x01, 0x02, 0x03 }.ToStream();
        Assert.False((await _validator.IsValid(stream, ".xyz")).IsValid);
    }

    [Fact]
    public async Task IsValidAsync_UnknownExtension_ReturnsUnknownExtension() {
        await using var stream = new byte[] { 0x00, 0x01, 0x02, 0x03 }.ToStream();
        Assert.False((await _validator.IsValid(stream, ".xyz")).IsValid);
    }

    [Theory]
    [InlineData(".gif", new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 })]
    [InlineData(".gif", new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 })]
    public async Task IsValid_GifVariants_ReturnsValid(string extension, byte[] bytes) {
        using var stream = bytes.ToStream();
        Assert.True((await _validator.IsValid(stream, extension)).IsValid);
    }

    [Theory]
    [InlineData(".gif", new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 })]
    [InlineData(".gif", new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 })]
    public async Task IsValidAsync_GifVariants_ReturnsValid(string extension, byte[] bytes) {
        await using var stream = bytes.ToStream();
        Assert.True((await _validator.IsValid(stream, extension)).IsValid);
    }

    [Fact]
    public async Task IsValid_ValidWebP_ReturnsValid() {
        using var stream = BuildWebPBytes().ToStream();
        Assert.True((await _validator.IsValid(stream, ".webp")).IsValid);
    }

    [Fact]
    public async Task IsValidAsync_ValidWebP_ReturnsValid() {
        await using var stream = BuildWebPBytes().ToStream();
        Assert.True((await _validator.IsValid(stream, ".webp")).IsValid);
    }

    [Fact]
    public async Task IsValid_WebPWithRiffButNoWebpMarker_ReturnsFailure() {
        using var stream = BuildWaveBytes().ToStream();
        Assert.False((await _validator.IsValid(stream, ".webp")).IsValid);
    }

    [Fact]
    public async Task IsValidAsync_WebPWithRiffButNoWebpMarker_ReturnsFailure() {
        await using var stream = BuildWaveBytes().ToStream();
        Assert.False((await _validator.IsValid(stream, ".webp")).IsValid);
    }

    [Theory]
    [InlineData("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\"></svg>")]
    public async Task IsValid_ValidSvg_ReturnsValid(string svgContent) {
        using var stream = Encoding.UTF8.GetBytes(svgContent).ToStream();
        Assert.True((await _validator.IsValid(stream, ".svg")).IsValid);
    }

    [Theory]
    [InlineData("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\"></svg>")]
    public async Task IsValidAsync_ValidSvg_ReturnsValid(string svgContent) {
        await using var stream = Encoding.UTF8.GetBytes(svgContent).ToStream();
        var result = await _validator.IsValid(stream, ".svg");
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task IsValid_SvgWithBom_ReturnsValid() {
        using var stream = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("<svg></svg>")).ToArray().ToStream();
        Assert.True((await _validator.IsValid(stream, ".svg")).IsValid);
    }

    [Fact]
    public async Task IsValidAsync_SvgWithBom_ReturnsValid() {
        await using var stream = Encoding.UTF8.GetBytes("<svg></svg>").ToStream();
        var result = await _validator.IsValid(stream, ".svg");
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task IsValid_InvalidSvg_ReturnsFailure() {
        using var stream = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }.ToStream();
        Assert.False((await _validator.IsValid(stream, ".svg")).IsValid);
    }

    [Fact]
    public async Task IsValidAsync_InvalidSvg_ReturnsFailure() {
        await using var stream = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }.ToStream();
        var result = await _validator.IsValid(stream, ".svg");
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task IsValid_ExtensionWithoutLeadingDot_StillValidates() {
        using var stream = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }.ToStream();
        Assert.True((await _validator.IsValid(stream, "jpg")).IsValid);
    }

    [Fact]
    public async Task IsValidAsync_ExtensionWithoutLeadingDot_StillValidates() {
        await using var stream = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }.ToStream();
        var result = await _validator.IsValid(stream, "jpg");
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task IsValid_NullOrEmptyExtension_ReturnsValid() {
        using var stream = new byte[] { 0x00, 0x01 }.ToStream();
        Assert.True((await _validator.IsValid(stream, "")).IsValid);
    }

    [Fact]
    public async Task IsValidAsync_NullOrEmptyExtension_ReturnsValid() {
        await using var stream = new byte[] { 0x00, 0x01 }.ToStream();
        var result = await _validator.IsValid(stream, "");
        Assert.True(result.IsValid);
    }

    private static byte[] BuildWebPBytes() {
        var bytes = new byte[12];
        bytes[0] = 0x52; bytes[1] = 0x49; bytes[2] = 0x46; bytes[3] = 0x46;
        bytes[4] = 0x00; bytes[5] = 0x00; bytes[6] = 0x00; bytes[7] = 0x00;
        bytes[8] = 0x57; bytes[9] = 0x45; bytes[10] = 0x42; bytes[11] = 0x50;
        return bytes;
    }

    private static byte[] BuildWaveBytes() {
        var bytes = new byte[12];
        bytes[0] = 0x52; bytes[1] = 0x49; bytes[2] = 0x46; bytes[3] = 0x46;
        bytes[4] = 0x00; bytes[5] = 0x00; bytes[6] = 0x00; bytes[7] = 0x00;
        bytes[8] = 0x57; bytes[9] = 0x41; bytes[10] = 0x56; bytes[11] = 0x45;
        return bytes;
    }
}