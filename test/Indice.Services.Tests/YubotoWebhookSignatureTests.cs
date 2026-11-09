using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Indice.Services.Tests;

public sealed class YubotoWebhookSignatureTests
{
    private const string WebhookSecret = "Tx43Joi7HFw8k9K2Yqa1DPy5t6W0Zsm7";
    private const string Reference = "ref123";
    private const string Timestamp = "1732184001";

    [Fact]
    public async Task ValidBase64UrlSignature_IsAccepted() {
        var signature = Convert.ToBase64String(ComputeHmac()).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var result = await ParseAsync(signature);

        Assert.True(result.Success);
        Assert.Single(result.Events);
    }

    [Fact]
    public async Task HexSignature_IsRejected() {
        var signature = Convert.ToHexString(ComputeHmac());

        var result = await ParseAsync(signature);

        Assert.False(result.Success);
        Assert.Equal(400, result.Status);
    }

    [Fact]
    public async Task TamperedSignature_IsRejected() {
        var hash = ComputeHmac();
        hash[0] ^= 0xFF;
        var signature = Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var result = await ParseAsync(signature);

        Assert.False(result.Success);
        Assert.Equal(400, result.Status);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0123456789abcdef0123456789abcdef")]
    public async Task WrongLengthSignature_IsRejected(string signature) {
        var result = await ParseAsync(signature);

        Assert.False(result.Success);
        Assert.Equal(400, result.Status);
    }

    private static byte[] ComputeHmac() {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WebhookSecret));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes($"{Reference}:{Timestamp}"));
    }

    private static Task<WebhookHttpResult> ParseAsync(string signature) {
        var options = new Mock<IOptionsSnapshot<SmsServiceYubotoOmniSettings>>();
        options.Setup(x => x.Value).Returns(new SmsServiceYubotoOmniSettings { WebhookSecret = WebhookSecret });
        var handler = new YubotoWebhookHttpHandler(options.Object);
        var parameters = new Dictionary<string, StringValues> {
            ["smsid"] = "msg-test-123",
            ["receiver"] = "306900000000",
            ["option1"] = signature,
            ["option2"] = $"{Reference}_{Timestamp}"
        };
        return handler.ParseAsync(new Dictionary<string, StringValues>(), parameters, Stream.Null, TestContext.Current.CancellationToken);
    }
}
