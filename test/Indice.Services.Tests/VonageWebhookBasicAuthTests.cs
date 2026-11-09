using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Indice.Services.Tests;

public sealed class VonageWebhookBasicAuthTests
{
    [Fact]
    public async Task ValidBasicAuth_IsAccepted_WhenCredentialsExist() {
        var headers = CreateBasicAuthHeaders("user", "pass");
        var result = await ParseAsync(CreateBaseParameters(), webhookUsername: "user", webhookPassword: "pass", requestHeaders: headers);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        var @event = result.Events[0];
        Assert.Equal("Vonage", @event.Provider);
        Assert.Equal("msg-test-123", @event.MessageId);
        Assert.Equal("306900000000", @event.Recipient);
        Assert.Equal("Indice", @event.Subject);
    }

    [Fact]
    public async Task FormUrlEncodedBodyPayload_IsParsed_WhenQueryIsEmpty() {
        var payload = "msisdn=306900000000&to=Indice&messageId=msg-test-form&status=delivered&message-timestamp=2024-11-21+10%3A13%3A21";
        await using var body = new MemoryStream(Encoding.UTF8.GetBytes(payload));

        var requestHeaders = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase) {
            ["Content-Type"] = "application/x-www-form-urlencoded"
        };

        var result = await ParseAsync(new Dictionary<string, StringValues>(), requestBody: body, requestHeaders: requestHeaders);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        var @event = result.Events[0];
        Assert.Equal("msg-test-form", @event.MessageId);
        Assert.Equal("306900000000", @event.Recipient);
        Assert.Equal("Indice", @event.Subject);
    }

    [Fact]
    public async Task JsonBodyPayload_IsParsed_WhenQueryIsEmpty() {
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

        var result = await ParseAsync(new Dictionary<string, StringValues>(), requestBody: body);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        var @event = result.Events[0];
        Assert.Equal("msg-test-body", @event.MessageId);
        Assert.Equal("306900000000", @event.Recipient);
        Assert.Equal("Indice", @event.Subject);
    }

    [Fact]
    public async Task MissingBasicAuth_IsRejected_WhenCredentialsExist() {
        var result = await ParseAsync(CreateBaseParameters(), webhookUsername: "user", webhookPassword: "pass");

        Assert.False(result.Success);
        Assert.Equal(401, result.Status);
    }

    [Fact]
    public async Task InvalidBasicAuth_IsRejected_WhenCredentialsExist() {
        var headers = CreateBasicAuthHeaders("user", "wrong");
        var result = await ParseAsync(CreateBaseParameters(), webhookUsername: "user", webhookPassword: "pass", requestHeaders: headers);

        Assert.False(result.Success);
        Assert.Equal(401, result.Status);
    }

    [Fact]
    public async Task Signature_IsIgnored_WhenBasicAuthIsValid() {
        var parameters = CreateBaseParameters();
        parameters["sig"] = "DEADBEEF";
        var headers = CreateBasicAuthHeaders("user", "pass");
        var result = await ParseAsync(parameters, webhookUsername: "user", webhookPassword: "pass", requestHeaders: headers);

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

    private static Dictionary<string, StringValues> CreateBasicAuthHeaders(string username, string password) {
        var raw = $"{username}:{password}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        return new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase) {
            ["Authorization"] = $"Basic {encoded}"
        };
    }

    private static Task<WebhookHttpResult> ParseAsync(
        Dictionary<string, StringValues> parameters,
        string? webhookUsername = null,
        string? webhookPassword = null,
        Stream? requestBody = null,
        Dictionary<string, StringValues>? requestHeaders = null) {
        var options = new Mock<IOptionsSnapshot<SmsServiceVonageSettings>>();
        options.Setup(x => x.Value).Returns(new SmsServiceVonageSettings {
            WebhookUsername = webhookUsername,
            WebhookPassword = webhookPassword
        });

        var handler = new VonageWebhookHttpHandler(options.Object);

        return handler.ParseAsync(requestHeaders ?? new Dictionary<string, StringValues>(), parameters, requestBody ?? Stream.Null, TestContext.Current.CancellationToken);
    }
}
