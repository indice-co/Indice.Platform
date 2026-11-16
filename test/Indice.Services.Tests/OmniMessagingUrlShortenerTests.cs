using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Indice.Services.Tests;

public sealed class OmniMessagingUrlShortenerTests
{
    [Theory]
    [InlineData(SmsServiceOmniMessagingMode.SmsOnly)]
    [InlineData(SmsServiceOmniMessagingMode.ViberOnly)]
    [InlineData(SmsServiceOmniMessagingMode.ViberWithSmsFallback)]
    public async Task SendAsync_UrlShortenerEnabled_WrapsUrlsInActiveChannels(SmsServiceOmniMessagingMode mode) {
        using var payload = await SendAsync(
            "Visit https://www.example.com! Manage (http://my.example.org/account?a=1&amp;b=2). Again https://www.example.com!",
            mode,
            enableUrlShortener: true);

        AssertContent(payload.RootElement.GetProperty("content"), mode, "Visit {https://www.example.com}! Manage ({http://my.example.org/account?a=1&amp;b=2}). Again {https://www.example.com}!");
        var recipient = Assert.Single(payload.RootElement.GetProperty("recipients").EnumerateArray());
        Assert.Equal(2, recipient.EnumerateObject().Count());
        Assert.Equal("306900000000", recipient.GetProperty("unique_id").GetString());
        Assert.Equal("306900000000", recipient.GetProperty("mobile").GetString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    public async Task SendAsync_UrlShortenerDisabledOrDefault_PreservesUrls(bool? enableUrlShortener) {
        const string message = "Visit https://www.example.com! Or http://my.example.org.";
        using var payload = await SendAsync(message, SmsServiceOmniMessagingMode.ViberWithSmsFallback, enableUrlShortener);

        AssertContent(payload.RootElement.GetProperty("content"), SmsServiceOmniMessagingMode.ViberWithSmsFallback, message);
    }

    [Theory]
    [InlineData("Click in https://eaxmple.com to TEST", "Click in {https://eaxmple.com} to TEST")]
    [InlineData("No links here.", "No links here.")]
    [InlineData("Existing {url} placeholder and www.example.com.", "Existing {url} placeholder and www.example.com.")]
    [InlineData("Visit ftp://example.com/file.", "Visit ftp://example.com/file.")]
    public async Task SendAsync_UrlShortenerEnabled_FormatsText(string message, string expectedText) {
        using var payload = await SendAsync(message, SmsServiceOmniMessagingMode.ViberWithSmsFallback, enableUrlShortener: true);

        AssertContent(payload.RootElement.GetProperty("content"), SmsServiceOmniMessagingMode.ViberWithSmsFallback, expectedText);
    }

    private static void AssertContent(JsonElement content, SmsServiceOmniMessagingMode mode, string expectedText) {
        if (mode is SmsServiceOmniMessagingMode.SmsOnly or SmsServiceOmniMessagingMode.ViberWithSmsFallback) {
            var sms = content.GetProperty("sms");
            Assert.Equal(expectedText, sms.GetProperty("text").GetString());
            Assert.Equal("INDICE", sms.GetProperty("from").GetString());
            Assert.Equal("UTF-8", sms.GetProperty("charset").GetString());
            Assert.Equal(3, sms.EnumerateObject().Count());
        } else {
            Assert.False(content.TryGetProperty("sms", out _));
        }
        if (mode is SmsServiceOmniMessagingMode.ViberOnly or SmsServiceOmniMessagingMode.ViberWithSmsFallback) {
            var message = content.GetProperty("viber").GetProperty("message");
            Assert.Equal(expectedText, message.GetProperty("text").GetString());
            Assert.Single(message.EnumerateObject());
        } else {
            Assert.False(content.TryGetProperty("viber", out _));
        }
    }

    private static async Task<JsonDocument> SendAsync(string message, SmsServiceOmniMessagingMode mode, bool? enableUrlShortener) {
        var settings = new SmsServiceOmniMessagingSettings {
            ApiKey = "test-api-key",
            AccountId = "test-account",
            Sender = "INDICE",
            Channel = mode
        };
        if (enableUrlShortener.HasValue) {
            settings.EnableUrlShortener = enableUrlShortener.Value;
        }
        var options = new Mock<IOptionsSnapshot<SmsServiceOmniMessagingSettings>>();
        options.Setup(x => x.Value).Returns(settings);
        using var handler = new CaptureHandler();
        using var client = new HttpClient(handler);
        var service = new SmsServiceOmniMessaging(client, options.Object, NullLogger<SmsServiceOmniMessaging>.Instance);

        var receipt = await service.SendAsync("+306900000000", "Subject", message);

        Assert.NotNull(receipt);
        Assert.NotNull(handler.RequestBody);
        return JsonDocument.Parse(handler.RequestBody);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("{\"campaign_id\":\"test-campaign\"}", Encoding.UTF8, "application/json")
            };
        }
    }
}
