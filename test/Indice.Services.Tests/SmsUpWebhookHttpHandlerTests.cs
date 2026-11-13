using System.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Indice.Services.Tests;

public class SmsUpWebhookHttpHandlerTests {
    private const string ReportUrlWithSignature = "https://api.example.com/webhooks/smsup/report?ref=abc123&ts=1715871124&signature=9f";
    private const string WebhookSecret = "YOUR_SECRET_TOKEN";

    [Fact]
    public async Task QueryParameters_AreParsed() {
        var parameters = CreateBaseParameters();
        AddSignature(parameters);

        var result = await ParseAsync(parameters, reportUrl: ReportUrlWithSignature, webhookSecret: WebhookSecret);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("123456", result.Events[0].MessageId);
        Assert.Equal("+306900000002", result.Events[0].Recipient);
        Assert.Equal("Status:DELIVRD", result.Events[0].EventType);
        Assert.True(result.Events[0].Succeeded);
    }

    [Theory]
    [InlineData("DELIVRD", true)]
    [InlineData("ACCEPTD", false)]
    [InlineData("EXPIRED", false)]
    [InlineData("DELETED", false)]
    [InlineData("UNDELIV", false)]
    [InlineData("UNKNOWN", false)]
    [InlineData("REJECTD", false)]
    public async Task DocumentedStatuses_AreParsed_WithCorrectSuccessState(string status, bool succeeded) {
        var parameters = CreateBaseParameters();
        parameters["status"] = status;

        var result = await ParseAsync(parameters);

        Assert.True(result.Success);
        var webhookEvent = Assert.Single(result.Events);
        Assert.Equal($"Status:{status}", webhookEvent.EventType);
        Assert.Equal(succeeded, webhookEvent.Succeeded);
    }

    [Fact]
    public async Task FormUrlEncodedBody_IsParsed_WhenQueryIsEmpty() {
        var bodyParameters = CreateBaseParameters().ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.Ordinal);
        var payload = string.Join("&", bodyParameters.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        await using var body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        var signatureParameters = CreateSignatureParameters();
        var headers = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase) {
            ["Content-Type"] = "application/x-www-form-urlencoded",
            ["x-test"] = "true"
        };
        var queryParameters = signatureParameters.ToDictionary(x => x.Key, x => new StringValues(x.Value), StringComparer.OrdinalIgnoreCase);

        var result = await ParseAsync(queryParameters, requestBody: body, requestHeaders: headers, reportUrl: ReportUrlWithSignature, webhookSecret: WebhookSecret);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("123456", result.Events[0].MessageId);
    }

    [Fact]
    public async Task MissingMessageId_ReturnsBadRequest() {
        var parameters = new Dictionary<string, StringValues> {
            ["to"] = "+306900000002"
        };
        AddSignature(parameters);

        var result = await ParseAsync(parameters, reportUrl: ReportUrlWithSignature, webhookSecret: WebhookSecret);

        Assert.False(result.Success);
        Assert.Equal(400, result.Status);
    }

    [Fact]
    public async Task MissingSignature_ReturnsForbidden_WhenWebhookSecretIsConfigured() {
        var parameters = CreateBaseParameters();

        var result = await ParseAsync(parameters, reportUrl: ReportUrlWithSignature, webhookSecret: WebhookSecret);

        Assert.False(result.Success);
        Assert.Equal(403, result.Status);
    }

    [Fact]
    public async Task ValidSignature_IsAccepted_WhenWebhookSecretIsConfigured() {
        var parameters = CreateBaseParameters();
        AddSignature(parameters);

        var result = await ParseAsync(parameters, webhookSecret: WebhookSecret);

        Assert.True(result.Success);
        Assert.Single(result.Events);
    }

    [Fact]
    public async Task InvalidSignature_ReturnsForbidden_WhenWebhookSecretIsConfigured() {
        var parameters = CreateBaseParameters();
        parameters["ref"] = "abc123";
        parameters["ts"] = "1715871124";
        parameters["signature"] = "deadbeef";

        var result = await ParseAsync(parameters, reportUrl: ReportUrlWithSignature, webhookSecret: WebhookSecret);

        Assert.False(result.Success);
        Assert.Equal(403, result.Status);
    }

    private static Dictionary<string, StringValues> CreateBaseParameters() => new(StringComparer.OrdinalIgnoreCase) {
        ["sms_id"] = "123456",
        ["status"] = "DELIVRD",
        ["from"] = "+306900000001",
        ["to"] = "+306900000002",
        ["timestamp"] = "1715871124"
    };

    private static Dictionary<string, string> CreateSignatureParameters() {
        var reference = "abc123";
        var timestamp = "1715871124";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WebhookSecret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{reference}:{timestamp}"))).ToLowerInvariant();
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["ref"] = reference,
            ["ts"] = timestamp,
            ["signature"] = signature
        };
    }

    private static void AddSignature(IDictionary<string, StringValues> parameters) {
        foreach (var parameter in CreateSignatureParameters()) {
            parameters[parameter.Key] = parameter.Value;
        }
    }

    private static Task<WebhookHttpResult> ParseAsync(
        Dictionary<string, StringValues> parameters,
        Stream? requestBody = null,
        Dictionary<string, StringValues>? requestHeaders = null,
        string? reportUrl = null,
        string? webhookSecret = null) {
        var options = new Mock<IOptionsSnapshot<SmsServiceSmsUpSettings>>();
        options.Setup(x => x.Value).Returns(new SmsServiceSmsUpSettings {
            ReportUrl = reportUrl,
            WebhookSecret = webhookSecret
        });

        var handler = new SmsUpWebhookHttpHandler(options.Object);

        return handler.ParseAsync(requestHeaders ?? new Dictionary<string, StringValues>(), parameters, requestBody ?? Stream.Null, TestContext.Current.CancellationToken);
    }
}
