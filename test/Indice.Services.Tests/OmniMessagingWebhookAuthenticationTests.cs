using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Indice.Services.Tests;

public sealed class OmniMessagingWebhookAuthenticationTests
{
    private const string Payload = """
        {
          "id": 1735689600000000,
          "campaign_id": 2501000000000001,
          "campaign_reference": "ORDER-12345",
          "message_id": 2501010000000001,
          "to": "306900000000",
          "type": "sms",
          "delivery_report": {
            "status": "delivered",
            "date": "2025-01-01T00:00:00Z"
          }
        }
        """;

    [Fact]
    public async Task ApiKeyAuthentication_ValidHeader_IsAccepted() {
        var settings = new SmsServiceOmniMessagingSettings {
            WebhookAuthenticationMethod = SmsServiceOmniMessagingWebhookAuthenticationMethod.ApiKey,
            WebhookApiKeyHeaderName = "X-API-Key",
            WebhookSecret = "my-api-key"
        };

        var headers = new Dictionary<string, StringValues> {
            ["X-API-Key"] = "my-api-key"
        };

        var result = await ParseAsync(settings, headers);

        Assert.True(result.Success);
        var webhookEvent = Assert.Single(result.Events);
        Assert.True(webhookEvent.Succeeded);
    }

    [Theory]
    [InlineData("open")]
    [InlineData("click")]
    public async Task InteractionEvents_AreMarkedSuccessful(string interaction) {
        var settings = new SmsServiceOmniMessagingSettings {
            WebhookAuthenticationMethod = SmsServiceOmniMessagingWebhookAuthenticationMethod.ApiKey,
            WebhookApiKeyHeaderName = "X-API-Key",
            WebhookSecret = "my-api-key"
        };
        var headers = new Dictionary<string, StringValues> {
            ["X-API-Key"] = "my-api-key"
        };
        var payload = $$"""
            {
              "id": "event-123",
              "webhook_type": "interaction",
              "webhook_data": {
                "message_id": "message-123",
                "to": "306900000000",
                "interaction": "{{interaction}}"
              }
            }
            """;

        var result = await ParseAsync(settings, headers, payload);

        Assert.True(result.Success);
        var webhookEvent = Assert.Single(result.Events);
        Assert.Equal(interaction, webhookEvent.EventType);
        Assert.True(webhookEvent.Succeeded);
    }

    [Fact]
    public async Task BasicAuthentication_ValidCredentials_IsAccepted() {
        var settings = new SmsServiceOmniMessagingSettings {
            WebhookAuthenticationMethod = SmsServiceOmniMessagingWebhookAuthenticationMethod.Basic,
            WebhookUsername = "username",
            WebhookPassword = "password"
        };

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("username:password"));
        var headers = new Dictionary<string, StringValues> {
            ["Authorization"] = $"Basic {credentials}"
        };

        var result = await ParseAsync(settings, headers);

        Assert.True(result.Success);
        Assert.Single(result.Events);
    }

    private static Task<WebhookHttpResult> ParseAsync(SmsServiceOmniMessagingSettings settings, Dictionary<string, StringValues> headers, string payload = Payload) {
        var options = new Mock<IOptionsSnapshot<SmsServiceOmniMessagingSettings>>();
        options.Setup(x => x.Value).Returns(settings);
        var handler = new OmnimessagingWebhookHttpHandler(options.Object);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        return handler.ParseAsync(headers, [], stream, TestContext.Current.CancellationToken);
    }
}
