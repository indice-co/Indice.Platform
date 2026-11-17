using System.Text;
using Indice.Extensions;
using Xunit;

namespace Indice.Common.Tests;

public class UrlExtensionsTests
{
    // 0xFB 0xFF 0xBF encodes to "+/+/" in standard base64 and "-_-_" in base64url.
    private static readonly byte[] SpecialBytes = [0xFB, 0xFF, 0xBF];

    #region ToBase64UrlSafe (encoding)

    [Theory]
    [InlineData("hello", "aGVsbG8")]
    [InlineData("hell", "aGVsbA")]
    [InlineData("hel", "aGVs")]
    [InlineData("", "")]
    public void ToBase64UrlSafe_NormalBytes_OmitsPadding(string plain, string expected) {
        Assert.Equal(expected, Encoding.UTF8.GetBytes(plain).ToBase64UrlSafe());
    }

    [Fact]
    public void ToBase64UrlSafe_UsesUrlSafeAlphabet() {
        Assert.Equal("-_-_", SpecialBytes.ToBase64UrlSafe());
    }

    [Fact]
    public void ToBase64UrlSafe_Null() {
        byte[] bytes = null!;
        Assert.Throws<ArgumentNullException>(() => bytes.ToBase64UrlSafe());
    }

    #endregion

    #region FromBase64UrlSafe (decoding)

    [Theory]
    [InlineData("aGVsbG8", "hello")]
    [InlineData("aGVsbA", "hell")]
    [InlineData("aGVs", "hel")]
    public void FromBase64UrlSafe_ValidWithoutPadding(string encoded, string expected) {
        Assert.Equal(expected, Encoding.UTF8.GetString(encoded.FromBase64UrlSafe()));
    }

    [Theory]
    [InlineData("aGVsbG8=", "hello")]
    [InlineData("aGVsbA==", "hell")]
    public void FromBase64UrlSafe_ValidWithPadding(string encoded, string expected) {
        Assert.Equal(expected, Encoding.UTF8.GetString(encoded.FromBase64UrlSafe()));
    }

    [Fact]
    public void FromBase64UrlSafe_UrlSafeAlphabet() {
        Assert.Equal(SpecialBytes, "-_-_".FromBase64UrlSafe());
    }

    [Fact]
    public void FromBase64UrlSafe_WithWhitespace() {
        const string encoded = "aGVs bG8";
#if NET9_0_OR_GREATER
        Assert.Equal("hello", Encoding.UTF8.GetString(encoded.FromBase64UrlSafe()));
#else
        // Length is 8 (% 4 == 0) so the legacy path adds no padding although 7 significant chars exist.
        Assert.Throws<FormatException>(() => encoded.FromBase64UrlSafe());
#endif
    }

    [Fact]
    public void FromBase64UrlSafe_StandardAlphabet() {
        const string encoded = "+/+/";
        Assert.Equal(SpecialBytes, encoded.FromBase64UrlSafe());
    }

    [Fact]
    public void FromBase64UrlSafe_InvalidLength_Throws() {
        Assert.Throws<FormatException>(() => "aGVsb".FromBase64UrlSafe());
    }

    [Fact]
    public void FromBase64UrlSafe_Null() {
        string encoded = null!;
#if NET9_0_OR_GREATER
        Assert.Throws<ArgumentNullException>(() => encoded.FromBase64UrlSafe());
#else
        Assert.Throws<NullReferenceException>(() => encoded.FromBase64UrlSafe());
#endif
    }

    #endregion

    [Theory]
    [InlineData("hello")]
    [InlineData("Γειά σου κόσμε")]
    [InlineData("?a=1&b=2/3+4")]
    public void Base64UrlSafe_RoundTrip(string plain) {
        Assert.Equal(plain, plain.Base64UrlSafeEncode().Base64UrlSafeDecode());
    }
}
