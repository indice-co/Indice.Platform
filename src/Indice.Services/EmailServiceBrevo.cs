using System.Net.Mime;
using System.Net.Http.Headers;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Indice.Services;

/// <summary>
/// Brevo implementation for the email service abstraction.
/// https://developers.brevo.com/docs/send-a-transactional-email
/// </summary>
public class EmailServiceBrevo : IEmailService
{
    /// <summary>Represents the name of the Brevo service as a constant string value.</summary>
    public const string ServiceName = "Brevo";

    /// <summary>Creates a new instance of <see cref="EmailServiceBrevo"/>.</summary>
    /// <param name="settings">An instance of <see cref="EmailServiceBrevoSettings"/> used to initialize the service.</param>
    /// <param name="httpClient">The HTTP client to use (DI managed)</param>
    /// <param name="htmlRenderingEngine">This is an abstraction for the rendering engine.</param>
    public EmailServiceBrevo(
        IOptionsSnapshot<EmailServiceBrevoSettings> settings,
        HttpClient httpClient,
        IHtmlRenderingEngine htmlRenderingEngine) {
        Settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        Provider = new EmailProvider(ServiceName, new EmailSender(Settings.Sender!, Settings.SenderName));
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        HtmlRenderingEngine = htmlRenderingEngine ?? throw new ArgumentNullException(nameof(htmlRenderingEngine));
        if (HttpClient.BaseAddress == null) {
            HttpClient.BaseAddress = new Uri(Settings.Api.TrimEnd('/') + "/");
        }
        HttpClient.DefaultRequestHeaders.Add("api-key", Settings.ApiKey ?? throw new ArgumentNullException(nameof(Settings.ApiKey)));
    }

    private EmailServiceBrevoSettings Settings { get; }
    private HttpClient HttpClient { get; }
    /// <inheritdoc/>
    public IHtmlRenderingEngine HtmlRenderingEngine { get; }
    /// <inheritdoc/>
    public EmailProvider Provider { get; }

    /// <inheritdoc/>
    public async Task<SendReceipt> SendAsync(string[] recipients, string subject, string? body, EmailAttachment[]? attachments = null, EmailSender? from = null) {
        var serializerOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var bccRecipients = string.IsNullOrEmpty(Settings.BccRecipients)
            ? null
            : (Settings.BccRecipients ?? "").Split(';', ',', StringSplitOptions.RemoveEmptyEntries).Select(x => new BrevoEmailAddress { Email = x }).ToArray();
        var request = new BrevoRequest {
            Sender = new BrevoEmailAddress {
                Email = from?.Address ?? Settings.Sender,
                Name = from?.DisplayName ?? Settings.SenderName
            },
            Subject = subject,
            To = recipients.Select(recipient => new BrevoEmailAddress {
                Email = recipient
            }).ToArray(),
            Bcc = bccRecipients,
            Attachment = attachments is { Length: > 0 }
                ? attachments.Select(x => new BrevoAttachment {
                    Name = x.FileName,
                    Content = Convert.ToBase64String(x.Data)
                }).ToArray()
                : null,
            HtmlContent = body
        };

        var requestJson = JsonSerializer.Serialize(request, serializerOptions);
        var messageId = Guid.NewGuid().ToString();
        using (var content = new StringContent(requestJson, Encoding.UTF8, MediaTypeNames.Application.Json)) {
            var response = await HttpClient.PostAsync("smtp/email", content);
            var responseContentJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) {
                throw new EmailServiceException($"Brevo service could not send email to recipients '{string.Join(", ", recipients)}'. Error is: '{responseContentJson}'.");
            }
            
            if (!string.IsNullOrWhiteSpace(responseContentJson)) {
                var responseContent = JsonSerializer.Deserialize<BrevoResponse>(responseContentJson, serializerOptions);
                messageId = responseContent?.MessageId ?? messageId;
            }
        }

        return new SendReceipt(messageId, DateTimeOffset.UtcNow);
    }
}

/// <inheritdoc/>
public sealed class BrevoWebhookHttpHandler(IOptions<EmailServiceBrevoSettings> options) : IWebhookHttpHandler
{
    private readonly EmailServiceBrevoSettings _options = options.Value;

    /// <inheritdoc/>
    public async Task<WebhookHttpResult> ParseAsync(IDictionary<string, StringValues> requestHeaders, IEnumerable<KeyValuePair<string, StringValues>> requestParameters, Stream requestBody, CancellationToken cancellationToken) {
        if (!IsAuthorized(requestHeaders)) {
            return WebhookHttpResult.UnauthorizedRequest("Invalid webhook credentials");
        }

        JsonNode? payload;
        try {
            payload = await JsonNode.ParseAsync(requestBody, cancellationToken: cancellationToken);
        } catch (Exception) {
            return WebhookHttpResult.BadRequest("Invalid JSON payload");
        }

        if (payload is not JsonArray and not JsonObject) {
            return WebhookHttpResult.BadRequest("Expected a JSON object or array of webhook events");
        }

        var events = ParseEvents(payload).ToList();
        return WebhookHttpResult.Ok(events);
    }

    private IEnumerable<WebhookEvent> ParseEvents(JsonNode payload) {
        var items = payload switch {
            JsonObject item => new[] { item },
            JsonArray array => array.OfType<JsonObject>(),
            _ => Enumerable.Empty<JsonObject>()
        };

        foreach (var item in items) {
            var messageId = GetMessageId(item);
            if (string.IsNullOrWhiteSpace(messageId)) {
                continue;
            }

            var eventType = GetString(item, "event")
                            ?? GetString(item, "msg_status")
                            ?? GetString(item, "status")
                            ?? "unknown";

            var eventMoment = GetString(item, "ts_event")
                              ?? GetString(item, "ts_epoch")
                              ?? GetString(item, "ts")
                              ?? GetString(item, "date")
                              ?? string.Empty;
            var recipient = GetString(item, "email") ?? GetString(item, "to") ?? string.Empty;

            var eventId = $"{messageId}:{eventType}:{eventMoment}:{recipient}";

            yield return new WebhookEvent {
                Provider = EmailServiceBrevo.ServiceName,
                EventType = eventType,
                EventId = eventId,
                MessageId = messageId,
                Timestamp = GetTimestamp(item),
                Recipient = recipient,
                Subject = GetString(item, "subject"),
                Payload = item
            };
        }
    }

    private static string? GetMessageId(JsonObject item) {
        return GetString(item, "message-id")
               ?? GetString(item, "messageId")
               ?? GetString(item, "reference")
               ?? GetString(item, "id");
    }

    private static string? GetString(JsonObject data, string propertyName) {
        var value = data[propertyName];

        if (value is JsonObject jsonObject) {
            var firstValue = jsonObject
                .Select(x => x.Value)
                .FirstOrDefault(x => x is not null);

            if (firstValue is JsonValue nestedJsonValue && nestedJsonValue.TryGetValue<string>(out var nestedText)) {
                return nestedText;
            }

            return firstValue?.ToString();
        }

        return value is JsonValue jsonValue &&
               jsonValue.TryGetValue<string>(out var text)
            ? text
            : value?.ToString();
    }

    private static DateTimeOffset GetTimestamp(JsonObject data) {
        var timestampMs = GetString(data, "ts_epoch");
        if (long.TryParse(timestampMs, NumberStyles.Integer, CultureInfo.InvariantCulture, out var epoch) && epoch > 0) {
            return epoch > 9_999_999_999
                ? DateTimeOffset.FromUnixTimeMilliseconds(epoch)
                : DateTimeOffset.FromUnixTimeSeconds(epoch);
        }

        var timestampSeconds = GetString(data, "ts_event") ?? GetString(data, "ts");
        if (long.TryParse(timestampSeconds, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) && seconds > 0) {
            return DateTimeOffset.FromUnixTimeSeconds(seconds);
        }

        var date = GetString(data, "date");
        if (DateTimeOffset.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)) {
            return parsed;
        }

        return DateTimeOffset.UtcNow;
    }

    private bool IsAuthorized(IDictionary<string, StringValues> requestHeaders) {
        if (string.IsNullOrWhiteSpace(_options.WebhookUsername) ||
            string.IsNullOrWhiteSpace(_options.WebhookPassword)) {
            return false;
        }

        var authorization = requestHeaders
            .FirstOrDefault(header =>
                string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase)).Value.ToString();

        if (!AuthenticationHeaderValue.TryParse(
                authorization,
                out var headerValue) ||
            !string.Equals(
                headerValue.Scheme,
                "Basic",
                StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(headerValue.Parameter)) {
            return false;
        }

        byte[] credentials;

        try {
            credentials = Convert.FromBase64String(headerValue.Parameter);
        } catch (Exception) {
            return false;
        }

        var separator = Array.IndexOf(credentials, (byte)':');

        if (separator < 0) {
            return false;
        }

        var username = Encoding.UTF8.GetBytes(_options.WebhookUsername);
        var password = Encoding.UTF8.GetBytes(_options.WebhookPassword);

        var usernameMatches = CryptographicOperations.FixedTimeEquals(credentials.AsSpan(0, separator), username);
        var passwordMatches = CryptographicOperations.FixedTimeEquals(credentials.AsSpan(separator + 1), password);

        return usernameMatches & passwordMatches;
    }
}

/// <summary>Custom settings that are used to send emails via Brevo.</summary>
public class EmailServiceBrevoSettings
{
    /// <summary>The configuration section name.</summary>
    public const string Name = "Brevo";
    /// <summary>The default sender address (ex. no-reply@indice.gr).</summary>
    public string Sender { get; set; } = null!;
    /// <summary>The default sender name (ex. INDICE SA)</summary>
    public string? SenderName { get; set; }
    /// <summary>Optional email addresses that are always added as blind carbon copy recipients.</summary>
    public string? BccRecipients { get; set; }
    /// <summary>The Brevo API key.</summary>
    public string? ApiKey { get; set; }
    /// <summary>The Brevo API URL (ex. https://api.brevo.com/v3/).</summary>
    public string Api { get; set; } = "https://api.brevo.com/v3/";
    /// <summary>The username used to authenticate incoming Brevo webhook requests.</summary>
    public string? WebhookUsername { get; set; }
    /// <summary>The password used to authenticate incoming Brevo webhook requests.</summary>
    public string? WebhookPassword { get; set; }
}

#region Brevo models

internal class BrevoRequest
{
    public BrevoEmailAddress? Sender { get; set; }
    public BrevoEmailAddress[]? To { get; set; }
    public BrevoEmailAddress[]? Bcc { get; set; }
    public BrevoAttachment[]? Attachment { get; set; }
    public string? Subject { get; set; }
    public string? HtmlContent { get; set; }
}

internal class BrevoResponse
{
    public string? MessageId { get; set; }
}


internal class BrevoEmailAddress
{
    public string? Email { get; set; }
    public string? Name { get; set; }
}

internal class BrevoAttachment
{
    public string? Content { get; set; }
    public string? Name { get; set; }
}
#endregion
