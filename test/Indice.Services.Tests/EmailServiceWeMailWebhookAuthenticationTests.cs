using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Indice.Services.Tests;

public sealed class EmailServiceWeMailWebhookAuthenticationTests
{
    private const string Payload = """
        {
          "id": "evt_123",
          "message_id": "msg_123",
          "event": "delivered",
          "to": "recipient@example.com",
          "subject": "Order update",
          "created_at": "2025-01-01T00:00:00Z"
        }
        """;

    [Fact]
    public async Task NoAuth_IsAccepted() {
        var settings = new EmailServiceWeMailSettings {
            WebhookAuthenticationMethod = EmailServiceWeMailWebhookAuthenticationMethod.NoAuth
        };

        var result = await ParseAsync(settings, new Dictionary<string, StringValues>());

        Assert.True(result.Success);
        Assert.Single(result.Events);
    }

    [Fact]
    public async Task BasicAuthentication_ValidCredentials_IsAccepted() {
        var settings = new EmailServiceWeMailSettings {
            WebhookAuthenticationMethod = EmailServiceWeMailWebhookAuthenticationMethod.Basic,
            WebhookUsername = "wemail",
            WebhookPassword = "secret"
        };

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("wemail:secret"));
        var headers = new Dictionary<string, StringValues> {
            ["Authorization"] = $"Basic {credentials}"
        };

        var result = await ParseAsync(settings, headers);

        Assert.True(result.Success);
        Assert.Single(result.Events);
    }

    [Fact]
    public async Task HmacAuthentication_ValidSignature_IsAccepted() {
        var secret = "whsec_test_secret";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var payloadToSign = $"{timestamp}.{Payload}";
        var digest = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payloadToSign));
        var signature = Convert.ToHexString(digest).ToLowerInvariant();

        var settings = new EmailServiceWeMailSettings {
            WebhookAuthenticationMethod = EmailServiceWeMailWebhookAuthenticationMethod.Hmac,
            WebhookSigningSecret = secret,
            WebhookSignatureHeaderName = "X-Wemail-Signature",
            WebhookReplayTolerance = TimeSpan.FromMinutes(5)
        };

        var headers = new Dictionary<string, StringValues> {
            ["X-Wemail-Signature"] = $"t={timestamp},v1={signature}"
        };

        var result = await ParseAsync(settings, headers);

        Assert.True(result.Success);
        Assert.Single(result.Events);
    }

    [Fact]
    public async Task HmacAuthentication_InvalidSignature_IsRejected() {
        var secret = "whsec_test_secret";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var settings = new EmailServiceWeMailSettings {
            WebhookAuthenticationMethod = EmailServiceWeMailWebhookAuthenticationMethod.Hmac,
            WebhookSigningSecret = secret,
            WebhookSignatureHeaderName = "X-Wemail-Signature",
            WebhookReplayTolerance = TimeSpan.FromMinutes(5)
        };

        var headers = new Dictionary<string, StringValues> {
            ["X-Wemail-Signature"] = $"t={timestamp},v1=deadbeef"
        };

        var result = await ParseAsync(settings, headers);

        Assert.False(result.Success);
        Assert.Equal(403, result.Status);
    }

    [Fact]
    public async Task HmacAuthentication_NonDocumentedFormat_IsRejected() {
        var secret = "whsec_test_secret";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var digest = HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{Payload}"));
        var signature = Convert.ToBase64String(digest);
        var settings = new EmailServiceWeMailSettings {
            WebhookAuthenticationMethod = EmailServiceWeMailWebhookAuthenticationMethod.Hmac,
            WebhookSigningSecret = secret
        };
        var headers = new Dictionary<string, StringValues> {
            ["X-Wemail-Signature"] = $"t={timestamp},v1={signature},kid=sk_test"
        };

        var result = await ParseAsync(settings, headers);

        Assert.False(result.Success);
        Assert.Equal(403, result.Status);
    }

    [Fact]
    public async Task HmacAuthentication_StaleTimestamp_IsRejected() {
        var secret = "whsec_test_secret";
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(-6).ToUnixTimeSeconds().ToString();
        var digest = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{Payload}"));
        var signature = Convert.ToHexString(digest).ToLowerInvariant();
        var settings = new EmailServiceWeMailSettings {
            WebhookAuthenticationMethod = EmailServiceWeMailWebhookAuthenticationMethod.Hmac,
            WebhookSigningSecret = secret
        };
        var headers = new Dictionary<string, StringValues> {
            ["X-Wemail-Signature"] = $"t={timestamp},v1={signature}"
        };

        var result = await ParseAsync(settings, headers);

        Assert.False(result.Success);
        Assert.Equal(403, result.Status);
    }

    [Fact]
    public async Task HmacAuthentication_WrongSecret_IsRejected() {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var digest = HMACSHA256.HashData(Encoding.UTF8.GetBytes("whsec_wrong_secret"), Encoding.UTF8.GetBytes($"{timestamp}.{Payload}"));
        var signature = Convert.ToHexString(digest).ToLowerInvariant();
        var settings = new EmailServiceWeMailSettings {
            WebhookAuthenticationMethod = EmailServiceWeMailWebhookAuthenticationMethod.Hmac,
            WebhookSigningSecret = "whsec_current_secret"
        };
        var headers = new Dictionary<string, StringValues> {
            ["X-Wemail-Signature"] = $"t={timestamp},v1={signature}"
        };

        var result = await ParseAsync(settings, headers);

        Assert.False(result.Success);
        Assert.Equal(403, result.Status);
    }

    [Fact]
    public async Task ParseEvents_NestedMessageId_IsPreferredOverEventId() {
        const string payload = """
            {"id":"evt_123","event":"delivered","data":{"message_id":"msg_123"}}
            """;

        var result = await ParseAsync(new EmailServiceWeMailSettings(), new Dictionary<string, StringValues>(), payload);

        Assert.True(result.Success);
        var webhookEvent = Assert.Single(result.Events);
        Assert.Equal("msg_123", webhookEvent.MessageId);
        Assert.Equal("evt_123", webhookEvent.EventId);
    }

    [Theory]
    [InlineData("{\"id\":\"evt_123\",\"event\":\"delivered\"}")]
    [InlineData("{\"event\":\"delivered\",\"data\":{\"id\":\"evt_123\"}}")]
    public async Task ParseEvents_WithoutMessageId_DoesNotUseAmbiguousId(string payload) {
        var result = await ParseAsync(new EmailServiceWeMailSettings(), new Dictionary<string, StringValues>(), payload);

        Assert.True(result.Success);
        Assert.Empty(result.Events);
    }

    private static Task<WebhookHttpResult> ParseAsync(EmailServiceWeMailSettings settings, Dictionary<string, StringValues> headers, string payload = Payload) {
        var options = new Mock<IOptions<EmailServiceWeMailSettings>>();
        options.Setup(x => x.Value).Returns(settings);
        var handler = new WeMailWebhookHttpHandler(options.Object);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        return handler.ParseAsync(headers, [], stream, TestContext.Current.CancellationToken);
    }
}
