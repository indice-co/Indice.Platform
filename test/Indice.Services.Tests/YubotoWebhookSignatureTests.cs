using System.Security.Cryptography;
using System.Text;
using Indice.Extensions;
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
        var signature = ComputeHmac().ToBase64UrlSafe();

        var result = await ParseAsync(signature);

        Assert.True(result.Success);
        Assert.Single(result.Events);
    }

    [Theory]
    [InlineData("1", "Delivered", "sms", true)]
    [InlineData("2", "Undelivered", "sms", false)]
    [InlineData("16", "Undelivered", "sms", false)]
    [InlineData("3", "Seen", "viber", true)]
    [InlineData("7", "Clicked", "viber", true)]
    [InlineData("9", "Subscribed", "viber", true)]
    [InlineData("10", "Unsubscribed", "viber", true)]
    [InlineData("-1", "Rejected", "sms", false)]
    [InlineData("-2", "Rejected", "sms", false)]
    [InlineData("-3", "Rejected", "sms", false)]
    [InlineData("-5", "Failed", "sms", false)]
    [InlineData("-6", "Expired", "sms", false)]
    [InlineData("-30", "Expired system", "sms", false)]
    [InlineData("-10", "Blacklisted", "viber", false)]
    [InlineData("-13", "Undelivered", "viber", false)]
    [InlineData("-23", "Undelivered", "viber", false)]
    [InlineData("-14", "Not delivered", "viber", false)]
    [InlineData("-26", "Error", "viber", false)]
    public async Task DocumentedStatuses_AreParsed_WithCorrectSuccessState(string status, string description, string channel, bool succeeded) {
        var result = await ParseAsync(ComputeHmac().ToBase64UrlSafe(), status, description, channel);

        Assert.True(result.Success);
        var webhookEvent = Assert.Single(result.Events);
        Assert.Equal(description, webhookEvent.EventType);
        Assert.Equal(succeeded, webhookEvent.Succeeded);
        Assert.Equal(status, webhookEvent.Payload?["status"]?.ToString());
        Assert.Equal(channel, webhookEvent.Payload?["channel"]?.ToString());
    }

    [Theory]
    [InlineData("1", "Undelivered", false)]
    [InlineData("-1", "Delivered", true)]
    public async Task StatusDescription_DeterminesSuccessRegardlessOfNumericCode(string status, string description, bool succeeded) {
        var result = await ParseAsync(ComputeHmac().ToBase64UrlSafe(), status, description, "sms");

        Assert.True(result.Success);
        Assert.Equal(succeeded, Assert.Single(result.Events).Succeeded);
    }

    [Fact]
    public async Task ExampleCallback_IsParsed() {
        var result = await ParseAsync(ComputeHmac().ToBase64UrlSafe(), "1", "Delivered", "sms", "2026-11-13 18:01:56", requireSignature: false);

        Assert.True(result.Success);
        var webhookEvent = Assert.Single(result.Events);
        Assert.Equal("31FC83EF-E009-4940-B44A-325BFCAA9F99", webhookEvent.MessageId);
        Assert.Equal("306946909041", webhookEvent.Recipient);
        Assert.Equal("INDICE", webhookEvent.Subject);
        Assert.Equal("Delivered", webhookEvent.EventType);
        Assert.True(webhookEvent.Succeeded);
        Assert.Equal(DateTimeOffset.Parse("2026-11-13 18:01:56"), webhookEvent.Timestamp);
        Assert.Equal("sms", webhookEvent.Payload?["channel"]?.ToString());
        Assert.Equal("0Sd8JFeH2V2NsbV2JZmvkPfJEswsUg7FRFOIJWEmVwI", webhookEvent.Payload?["option1"]?.ToString());
        Assert.Equal("B72CE73C_1794585713", webhookEvent.Payload?["option2"]?.ToString());
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
        var signature = hash.ToBase64UrlSafe();

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

    private static Task<WebhookHttpResult> ParseAsync(string signature, string? status = null, string? statusDescription = null, string? channel = null, string? dlrDate = null, bool requireSignature = true) {
        var options = new Mock<IOptionsSnapshot<SmsServiceYubotoOmniSettings>>();
        options.Setup(x => x.Value).Returns(new SmsServiceYubotoOmniSettings { WebhookSecret = requireSignature ? WebhookSecret : null });
        var handler = new YubotoWebhookHttpHandler(options.Object);
        var parameters = new Dictionary<string, StringValues> {
            ["smsid"] = status is null ? "msg-test-123" : "31FC83EF-E009-4940-B44A-325BFCAA9F99",
            ["receiver"] = status is null ? "306900000000" : "306946909041",
            ["option1"] = signature,
            ["option2"] = $"{Reference}_{Timestamp}"
        };
        if (status is not null) {
            parameters["status"] = status;
        }
        if (statusDescription is not null) {
            parameters["statusDescription"] = statusDescription;
        }
        if (channel is not null) {
            parameters["channel"] = channel;
        }
        if (dlrDate is not null) {
            parameters["dlrDate"] = dlrDate;
        }
        if (statusDescription == "Delivered") {
            parameters["sender"] = "INDICE";
            parameters["option1"] = "0Sd8JFeH2V2NsbV2JZmvkPfJEswsUg7FRFOIJWEmVwI";
            parameters["option2"] = "B72CE73C_1794585713";
        }
        return handler.ParseAsync(new Dictionary<string, StringValues>(), parameters, Stream.Null, TestContext.Current.CancellationToken);
    }
}
