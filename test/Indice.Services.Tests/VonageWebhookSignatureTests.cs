using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Indice.Services.Tests;

public sealed class VonageWebhookSignatureTests
{
    private const string SignatureSecret = "Tx43Joi7HFw8k9K2Yqa1DPy5t6W0Zsm7";

    [Fact]
    public async Task ValidSignature_IsAccepted() {
        var parameters = CreateBaseParameters();
        parameters["sig"] = CreateSignature(parameters, SignatureSecret);

        var result = await ParseAsync(parameters);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        var @event = result.Events[0];
        Assert.Equal("Vonage", @event.Provider);
        Assert.Equal("msg-test-123", @event.MessageId);
        Assert.Equal("306900000000", @event.Recipient);
        Assert.Equal("Indice", @event.Subject);
    }

    [Fact]
    public async Task JsonBodyPayload_IsParsed_WhenQueryIsEmpty_AndSecretIsEmpty() {
        var payload = """
        {
          "msisdn": "306900000000",
          "to": "Indice",
          "messageId": "msg-test-body",
          "status": "delivered",
          "message-timestamp": "2024-11-21 10:13:21"
        }
        """;
        await using var body = new MemoryStream(Encoding.UTF8.GetBytes(payload));

        var result = await ParseAsync(new Dictionary<string, StringValues>(), signatureSecret: string.Empty, requestBody: body);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        var @event = result.Events[0];
        Assert.Equal("msg-test-body", @event.MessageId);
        Assert.Equal("306900000000", @event.Recipient);
        Assert.Equal("Indice", @event.Subject);
    }

    [Fact]
    public async Task TamperedSignature_IsRejected() {
        var parameters = CreateBaseParameters();
        parameters["sig"] = "DEADBEEF";

        var result = await ParseAsync(parameters);

        Assert.False(result.Success);
        Assert.Equal(400, result.Status);
    }

    [Fact]
    public async Task MissingSignature_IsRejected_WhenSecretExists() {
        var result = await ParseAsync(CreateBaseParameters());

        Assert.False(result.Success);
        Assert.Equal(400, result.Status);
    }

    [Fact]
    public async Task MissingSignature_IsAccepted_WhenSecretIsEmpty() {
        var result = await ParseAsync(CreateBaseParameters(), signatureSecret: string.Empty);

        Assert.True(result.Success);
        Assert.Single(result.Events);
    }

    private static Dictionary<string, StringValues> CreateBaseParameters() => new(StringComparer.OrdinalIgnoreCase) {
        ["msisdn"] = "306900000000",
        ["to"] = "Indice",
        ["messageId"] = "msg-test-123",
        ["status"] = "delivered",
        ["message-timestamp"] = "2024-11-21 10:13:21"
    };

    private static string CreateSignature(Dictionary<string, StringValues> parameters, string secret, string signatureMethod = "hmac-sha256") {
        var queryToSign = string.Join("&", parameters
            .Where(kvp => !"sig".Equals(kvp.Key, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(kvp.Value.ToString()))
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .Select(kvp => $"{Sanitize(kvp.Key)}={Sanitize(kvp.Value.ToString())}"));

        var query = $"&{queryToSign}";
        var hash = signatureMethod.ToLowerInvariant() switch {
            "md5" => MD5.HashData(Encoding.UTF8.GetBytes($"{query}{secret}")),
            "hmac-md5" => HMACMD5.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(query)),
            "hmac-sha1" => HMACSHA1.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(query)),
            "hmac-sha256" => HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(query)),
            "hmac-sha512" => HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(query)),
            _ => HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(query)),
        };

        return Convert.ToHexString(hash);
    }

    private static string Sanitize(string value) => value.Replace('=', '_').Replace('&', '_');

    private static Task<WebhookHttpResult> ParseAsync(
        Dictionary<string, StringValues> parameters,
        string signatureSecret = SignatureSecret,
        Stream? requestBody = null) {
        var options = new Mock<IOptionsSnapshot<SmsServiceVonageSettings>>();
        options.Setup(x => x.Value).Returns(new SmsServiceVonageSettings {
            SignatureSecret = signatureSecret,
            SignatureMethod = "hmac-sha256"
        });

        var handler = new VonageWebhookHttpHandler(options.Object);

        return handler.ParseAsync(new Dictionary<string, StringValues>(), parameters, requestBody ?? Stream.Null, TestContext.Current.CancellationToken);
    }
}
