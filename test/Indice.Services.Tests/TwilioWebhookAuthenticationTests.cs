using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Indice.Services.Tests;

public sealed class TwilioWebhookAuthenticationTests
{
    private const string WebhookAuthToken = "twilio-webhook-token";
    private const string WebhookUrl = "https://example.com/webhooks/twilio";
    private const string WebhookUrlWithQuery = "https://example.com/webhooks/twilio?tenant=acme";

    [Fact]
    public async Task MissingSignature_IsRejected_WhenWebhookAuthTokenIsConfigured() {
        var result = await ParseAsync(CreateBaseParameters());

        Assert.False(result.Success);
        Assert.Equal(403, result.Status);
    }

    [Fact]
    public async Task MissingSignature_IsAccepted_WhenWebhookAuthTokenIsNotConfigured() {
        var result = await ParseAsync(CreateBaseParameters(), webhookAuthToken: null);

        Assert.True(result.Success);
        Assert.Single(result.Events);
    }

    [Fact]
    public async Task FormUrlEncodedBody_IsParsed_WhenQueryIsEmpty() {
        var bodyParameters = CreateBaseParameters().ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.Ordinal);
        var payload = string.Join("&", bodyParameters.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        await using var body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        var headers = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase) {
            ["Content-Type"] = "application/x-www-form-urlencoded"
        };

        var result = await ParseAsync(
            new Dictionary<string, StringValues>(),
            webhookAuthToken: null,
            requestBody: body,
            requestHeaders: headers);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("SM00000000000000000000000000000001", result.Events[0].MessageId);
    }

    [Fact]
    public async Task ValidSignature_IsAccepted_WhenWebhookUrlContainsQuery() {
        var formParameters = CreateBaseParameters();
        var queryParameters = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase) {
            ["tenant"] = "acme"
        };
        var payload = string.Join("&", formParameters.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value.ToString())}"));
        await using var body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        var headers = CreateSignatureHeaders(formParameters, WebhookUrlWithQuery);
        headers["Content-Type"] = "application/x-www-form-urlencoded";

        var result = await ParseAsync(
            queryParameters,
            requestBody: body,
            requestHeaders: headers,
            webhookUrl: WebhookUrlWithQuery);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("SM00000000000000000000000000000001", result.Events[0].MessageId);
    }

    private static Dictionary<string, StringValues> CreateBaseParameters() => new(StringComparer.OrdinalIgnoreCase) {
        ["MessageSid"] = "SM00000000000000000000000000000001",
        ["MessageStatus"] = "delivered",
        ["From"] = "+306900000001",
        ["To"] = "+306900000002",
        ["Body"] = "Hello from Twilio"
    };

    private static Dictionary<string, StringValues> CreateSignatureHeaders(
        Dictionary<string, StringValues> parameters,
        string webhookUrl = WebhookUrl) {
        var normalized = parameters.ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.Ordinal);
        var builder = new StringBuilder(webhookUrl);
        foreach (var parameter in normalized.OrderBy(x => x.Key, StringComparer.Ordinal)) {
            builder.Append(parameter.Key).Append(parameter.Value);
        }

        var signature = Convert.ToBase64String(
            HMACSHA1.HashData(
                Encoding.UTF8.GetBytes(WebhookAuthToken),
                Encoding.UTF8.GetBytes(builder.ToString())));

        return new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase) {
            ["X-Twilio-Signature"] = signature
        };
    }

    private static Task<WebhookHttpResult> ParseAsync(
        Dictionary<string, StringValues> parameters,
        string? webhookAuthToken = WebhookAuthToken,
        Stream? requestBody = null,
        Dictionary<string, StringValues>? requestHeaders = null,
        string webhookUrl = WebhookUrl) {
        var options = new Mock<IOptionsSnapshot<SmsServiceTwilioSettings>>();
        options.Setup(x => x.Value).Returns(new SmsServiceTwilioSettings {
            WebhookAuthToken = webhookAuthToken,
            WebhookUrl = webhookUrl
        });

        var handler = new TwilioWebhookHttpHandler(options.Object);

        return handler.ParseAsync(requestHeaders ?? new Dictionary<string, StringValues>(), parameters, requestBody ?? Stream.Null, TestContext.Current.CancellationToken);
    }
}
