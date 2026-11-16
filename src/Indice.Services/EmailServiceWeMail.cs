using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Indice.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Globalization;

namespace Indice.Services;

/// <summary>
/// WeMail service implementation for the email service abstraction.
/// Reference: https://wemail.io/docs/send
/// </summary>
public sealed class EmailServiceWeMail : IEmailService
{
    private EmailServiceWeMailSettings _settings { get; }

    private HttpClient _httpClient { get; }

    private ILogger<EmailServiceWeMail> _logger { get; }

    /// <summary>Represents the name of the WeMail service as a constant string value.</summary>
    public const string ServiceName = "WeMail";

    /// <summary>The endpoint that handles message services.</summary>
    public const string MessagesEndpoint = "messages";

    /// <inheritdoc/>
    public IHtmlRenderingEngine? HtmlRenderingEngine  { get; }

    /// <inheritdoc/>
    public EmailProvider Provider { get; }

    /// <summary>Creates a new instance of <see cref="EmailServiceWeMail"/>.</summary>
    /// <param name="settings">An instance of <see cref="EmailServiceWeMailSettings"/> used to initialize the service.</param>
    /// <param name="httpClient">The HTTP client to use (DI managed)</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <param name="htmlRenderingEngine">This is an abstraction for the rendering engine.</param>
    public EmailServiceWeMail(
        IOptionsSnapshot<EmailServiceWeMailSettings> settings,
        HttpClient httpClient,
        ILogger<EmailServiceWeMail> logger,
        IHtmlRenderingEngine htmlRenderingEngine
    ) {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Provider = new EmailProvider(ServiceName, new EmailSender(_settings.Sender, _settings.SenderName));
        HtmlRenderingEngine = htmlRenderingEngine ?? throw new ArgumentNullException(nameof(htmlRenderingEngine));
        _httpClient.BaseAddress = new Uri(_settings.Api.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
    }

    /// <inheritdoc/>
    public async Task<SendReceipt> SendAsync(string[] recipients, string subject, string? body, EmailAttachment[]? attachments = null, EmailSender? from = null) {
        var messageId = Guid.NewGuid().ToString();
        var bccRecipients = (_settings.BccRecipients ?? string.Empty)
            .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        ArgumentNullException.ThrowIfNull(body);
        using var request = new HttpRequestMessage(HttpMethod.Post, MessagesEndpoint) {
            Content = JsonContent.Create(
                 new WeMailMessageRequest {
                     From = from?.Address ?? _settings.Sender,
                     To = recipients,
                     Bcc = bccRecipients.Length > 0 ? bccRecipients : null,
                     Subject = subject,
                     Html = body,
                     Headers = new Dictionary<string, string> {
                         ["X-Message-Id"] = messageId
                     },
                     Tracking = new WeMailTracking {
                         Unsubscribe = _settings.TrackUnsubscribe
                     },
                     Attachments = attachments?
                         .Select(x => new WeMailAttachment {
                             Filename = x.FileName,
                             Content = Convert.ToBase64String(x.Data),
                             ContentType = FileExtensions.GetMimeType(Path.GetExtension(x.FileName))
                         })
                         .ToArray()
                 },
                 options: new JsonSerializerOptions {
                     PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                     DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                 })
        };


        using var responseMessage = await _httpClient.SendAsync(request);

        if (!responseMessage.IsSuccessStatusCode) {
            var content = await responseMessage.Content.ReadAsStringAsync();
            _logger.LogError("WeMail service could not send email to recipients '{recipients}'. Error is: '{content}'.", string.Join(", ", recipients), content);
            throw new EmailServiceException($"WeMail service could not send email to recipients '{string.Join(", ", recipients)}'. Error is: '{content}'.");
        }

        var responseJson = await responseMessage.Content.ReadAsStringAsync();
        var weMailResponse = JsonSerializer.Deserialize<WeMailSendAcknowledgement>(responseJson)!;

        return new SendReceipt(weMailResponse.Id ?? messageId, weMailResponse.CreatedAt);
    }
}

/// <summary>
/// Specifies the webhook authentication method used by WeMail callbacks.
/// </summary>
public enum EmailServiceWeMailWebhookAuthenticationMethod {
    /// <summary>
    /// Accepts webhook requests without authentication checks.
    /// </summary>
    NoAuth,

    /// <summary>
    /// Validates HTTP Basic credentials from the Authorization header.
    /// </summary>
    Basic,

    /// <summary>
    /// Validates an HMAC SHA-256 signature using the configured signing secret.
    /// </summary>
    Hmac
}

/// <summary>Custom settings that are used to send emails via WeMail REST API.</summary>
public sealed class EmailServiceWeMailSettings
{
    /// <summary>The configuration section name.</summary>
    public const string Name = EmailServiceWeMail.ServiceName;

    /// <summary>The default sender address (ex. no-reply@indice.gr).</summary>
    public string Sender { get; set; } = null!;

    /// <summary>The default sender name (ex. INDICE SA)</summary>
    public string? SenderName { get; set; }

    /// <summary>Optional email addresses that are always added as blind carbon copy recipients.</summary>
    public string? BccRecipients { get; set; }

    /// <summary>The API key.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Unsubscribe tracking preference. It's enabled by default.</summary>
    public bool TrackUnsubscribe { get; set; } = true;

    /// <summary>The WeMail API URL.</summary>
    public string Api { get; set; } = "https://api.wemail.io/v3/";

    /// <summary>
    /// Gets or sets the webhook authentication method configured in WeMail.
    /// </summary>
    public EmailServiceWeMailWebhookAuthenticationMethod WebhookAuthenticationMethod { get; set; } = EmailServiceWeMailWebhookAuthenticationMethod.NoAuth;

    /// <summary>
    /// Gets or sets the webhook username used when <see cref="WebhookAuthenticationMethod"/> is <see cref="EmailServiceWeMailWebhookAuthenticationMethod.Basic"/>.
    /// </summary>
    public string? WebhookUsername { get; set; }

    /// <summary>
    /// Gets or sets the webhook password used when <see cref="WebhookAuthenticationMethod"/> is <see cref="EmailServiceWeMailWebhookAuthenticationMethod.Basic"/>.
    /// </summary>
    public string? WebhookPassword { get; set; }

    /// <summary>
    /// Gets or sets the webhook signing secret used when <see cref="WebhookAuthenticationMethod"/> is <see cref="EmailServiceWeMailWebhookAuthenticationMethod.Hmac"/>.
    /// </summary>
    public string? WebhookSigningSecret { get; set; }

    /// <summary>
    /// Gets or sets the signature header name used by WeMail HMAC authentication.
    /// </summary>
    public string WebhookSignatureHeaderName { get; set; } = "X-Wemail-Signature";

    /// <summary>
    /// Gets or sets the optional timestamp header name used by WeMail HMAC authentication.
    /// </summary>
    public string WebhookTimestampHeaderName { get; set; } = "X-Wemail-Timestamp";

    /// <summary>
    /// Gets or sets the maximum accepted age for signed webhook requests.
    /// </summary>
    public TimeSpan WebhookReplayTolerance { get; set; } = TimeSpan.FromMinutes(5);
}

/// <inheritdoc/>
public sealed class WeMailWebhookHttpHandler(IOptions<EmailServiceWeMailSettings> options) : IWebhookHttpHandler
{
    private static readonly HashSet<string> SucceededStatuses = new(StringComparer.OrdinalIgnoreCase) {
        "queued",
        "sent",
        "delivered",
        "opened",
        "clicked",
        "unsubscribed",
        "resubscribed"
    };

    private readonly EmailServiceWeMailSettings _settings = options.Value;

    /// <inheritdoc/>
    public async Task<WebhookHttpResult> ParseAsync(
        IDictionary<string, StringValues> requestHeaders,
        IEnumerable<KeyValuePair<string, StringValues>> requestParameters,
        Stream requestBody,
        CancellationToken cancellationToken) {
        var payloadBytes = await ReadPayloadBytesAsync(requestBody, cancellationToken);

        if (!IsAuthorized(requestHeaders, payloadBytes)) {
            return WebhookHttpResult.ForbiddenRequest("Invalid WeMail webhook authentication.");
        }

        JsonNode? payload;
        try {
            payload = JsonNode.Parse(payloadBytes);
        } catch (JsonException) {
            return WebhookHttpResult.BadRequest("Invalid WeMail webhook JSON.");
        }

        if (payload is not JsonObject and not JsonArray) {
            return WebhookHttpResult.BadRequest("Expected a JSON object or array of WeMail webhook events.");
        }

        return WebhookHttpResult.Ok(ParseEvents(payload).ToList(), statusCode: 204);
    }

    private bool IsAuthorized(IDictionary<string, StringValues> requestHeaders, byte[] payloadBytes)
        => _settings.WebhookAuthenticationMethod switch {
            EmailServiceWeMailWebhookAuthenticationMethod.NoAuth => true,
            EmailServiceWeMailWebhookAuthenticationMethod.Basic => IsBasicAuthorized(requestHeaders),
            EmailServiceWeMailWebhookAuthenticationMethod.Hmac => IsHmacAuthorized(requestHeaders, payloadBytes),
            _ => false
        };

    private bool IsBasicAuthorized(IDictionary<string, StringValues> requestHeaders) {
        var hasUsername = !string.IsNullOrWhiteSpace(_settings.WebhookUsername);
        var hasPassword = !string.IsNullOrWhiteSpace(_settings.WebhookPassword);
        var incompleteCredentials = hasUsername != hasPassword;
        if (incompleteCredentials) {
            return false;
        }

        var authorization = requestHeaders
            .FirstOrDefault(header =>
                string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase)).Value.ToString();

        return WebhookAuthentication.IsValidBasicAuthentication(authorization, _settings.WebhookUsername!, _settings.WebhookPassword!);
    }

    private bool IsHmacAuthorized(IDictionary<string, StringValues> requestHeaders, byte[] payloadBytes) {
        if (string.IsNullOrWhiteSpace(_settings.WebhookSigningSecret)) {
            return false;
        }

        var signatureHeaderName = string.IsNullOrWhiteSpace(_settings.WebhookSignatureHeaderName)
            ? "X-Wemail-Signature"
            : _settings.WebhookSignatureHeaderName;
        if (!TryGetHeaderValue(requestHeaders, signatureHeaderName, out var signatureHeader) || string.IsNullOrWhiteSpace(signatureHeader)) {
            return false;
        }

        if (!TryParseSignatureHeader(signatureHeader, out var timestampRaw, out var signatures)) {
            return false;
        }

        if (timestampRaw is null) {
            var timestampHeaderName = string.IsNullOrWhiteSpace(_settings.WebhookTimestampHeaderName)
                ? "X-Wemail-Timestamp"
                : _settings.WebhookTimestampHeaderName;
            if (!TryGetHeaderValue(requestHeaders, timestampHeaderName, out var timestampHeaderValue) || string.IsNullOrWhiteSpace(timestampHeaderValue)) {
                return false;
            }
            timestampRaw = timestampHeaderValue;
        }

        if (!long.TryParse(timestampRaw, NumberStyles.None, CultureInfo.InvariantCulture, out var timestampSeconds)) {
            return false;
        }

        DateTimeOffset timestamp;
        try {
            timestamp = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);
        } catch (ArgumentOutOfRangeException) {
            return false;
        }

        var maxAge = _settings.WebhookReplayTolerance > TimeSpan.Zero
            ? _settings.WebhookReplayTolerance
            : TimeSpan.FromMinutes(5);
        if ((DateTimeOffset.UtcNow - timestamp).Duration() > maxAge) {
            return false;
        }

        var timestampPrefix = Encoding.UTF8.GetBytes($"{timestampRaw}.");
        var signedPayload = new byte[timestampPrefix.Length + payloadBytes.Length];
        Buffer.BlockCopy(timestampPrefix, 0, signedPayload, 0, timestampPrefix.Length);
        Buffer.BlockCopy(payloadBytes, 0, signedPayload, timestampPrefix.Length, payloadBytes.Length);

        var secretBytes = Encoding.UTF8.GetBytes(_settings.WebhookSigningSecret);
        var expectedSignature = HMACSHA256.HashData(secretBytes, signedPayload);
        foreach (var signature in signatures) {
            if (signature.Length != 64 || !signature.All(Uri.IsHexDigit)) {
                continue;
            }

            var providedSignature = Convert.FromHexString(signature);
            if (CryptographicOperations.FixedTimeEquals(expectedSignature, providedSignature)) {
                return true;
            }
        }

        return false;
    }

    private static bool TryParseSignatureHeader(string signatureHeader, out string? timestamp, out List<string> signatures) {
        timestamp = null;
        signatures = [];
        var hasTimestamp = false;

        foreach (var segment in signatureHeader.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)) {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex == segment.Length - 1) {
                continue;
            }

            var key = segment[..separatorIndex];
            var value = segment[(separatorIndex + 1)..];
            if (key.Equals("t", StringComparison.OrdinalIgnoreCase)) {
                if (hasTimestamp) {
                    timestamp = null;
                    signatures.Clear();
                    return false;
                }
                timestamp = value;
                hasTimestamp = true;
            } else if (key.Equals("v1", StringComparison.OrdinalIgnoreCase)) {
                signatures.Add(value);
            }
        }

        return signatures.Count > 0;
    }

    private static bool TryGetHeaderValue(IDictionary<string, StringValues> headers, string headerName, out string value) {
        foreach (var (key, headerValue) in headers) {
            if (string.Equals(key, headerName, StringComparison.OrdinalIgnoreCase)) {
                value = headerValue.ToString();
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    //TODO: Use when we implement the webhook event filtering based on the event type.
    private static bool EventSucceeded(string eventType) {
        return SucceededStatuses.Contains(eventType);
    }

    private static IEnumerable<WebhookEvent> ParseEvents(JsonNode payload) {
        var items = payload switch {
            JsonObject item => new[] { item },
            JsonArray array => array.OfType<JsonObject>(),
            _ => Enumerable.Empty<JsonObject>()
        };

        foreach (var item in items) {
            var messageId = GetString(item, "message_id")
                            ?? GetString(item, "messageId")
                            ?? GetString(item, "data.message_id");

            if (string.IsNullOrWhiteSpace(messageId)) {
                continue;
            }

            var eventType = GetString(item, "event")
                            ?? GetString(item, "type")
                            ?? GetString(item, "status")
                            ?? GetString(item, "data.status")
                            ?? "unknown";

            var timestamp = GetTimestamp(item);
            var eventId = GetString(item, "event_id")
                          ?? GetString(item, "webhook_id")
                          ?? GetString(item, "id")
                          ?? $"{messageId}:{eventType}:{timestamp:O}";

            var recipient = GetString(item, "to")
                            ?? GetString(item, "recipient")
                            ?? GetString(item, "email")
                            ?? GetString(item, "data.to")
                            ?? string.Empty;

            yield return new WebhookEvent {
                Provider = EmailServiceWeMail.ServiceName,
                EventType = eventType,
                EventId = eventId,
                MessageId = messageId,
                Timestamp = timestamp,
                Recipient = recipient,
                Subject = GetString(item, "subject")
                          ?? GetString(item, "data.subject")
                          ?? GetString(item, "campaign"),
                Payload = item.DeepClone()
            };
        }
    }

    private static DateTimeOffset GetTimestamp(JsonObject item) {
        var raw = GetString(item, "timestamp")
                  ?? GetString(item, "created_at")
                  ?? GetString(item, "date")
                  ?? GetString(item, "data.timestamp");

        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var epoch) && epoch > 0 && epoch <= DateTimeOffset.MaxValue.ToUnixTimeMilliseconds()) {
            return epoch > 9_999_999_999
                ? DateTimeOffset.FromUnixTimeMilliseconds(epoch)
                : DateTimeOffset.FromUnixTimeSeconds(epoch);
        }

        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)) {
            return parsed;
        }

        return DateTimeOffset.UtcNow;
    }

    private static string? GetString(JsonObject item, string path) {
        JsonNode? current = item;
        foreach (var segment in path.Split('.')) {
            if (current is not JsonObject currentObject || !currentObject.TryGetPropertyValue(segment, out current)) {
                return null;
            }
        }

        return current switch {
            null => null,
            JsonValue value when value.TryGetValue<string>(out var text) => text,
            JsonValue value => value.ToString(),
            _ => current.ToJsonString()
        };
    }

    private static async Task<byte[]> ReadPayloadBytesAsync(Stream requestBody, CancellationToken cancellationToken) {
        if (requestBody is null || requestBody == Stream.Null) {
            return [];
        }

        if (requestBody.CanSeek) {
            requestBody.Position = 0;
        }

        using var buffer = new MemoryStream();
        await requestBody.CopyToAsync(buffer, cancellationToken);

        if (requestBody.CanSeek) {
            requestBody.Position = 0;
        }

        return buffer.ToArray();
    }
}

internal sealed class WeMailMessageRequest
{
    /// <summary>The sender email address.</summary>
    public required string From { get; init; }

    /// <summary>The primary recipients.</summary>
    public required string[] To { get; init; }

    /// <summary>The CC recipients.</summary>
    public IReadOnlyList<string>? Cc { get; init; }

    /// <summary>The BCC recipients.</summary>
    public IReadOnlyList<string>? Bcc { get; init; }

    /// <summary>The email subject.</summary>
    public required string Subject { get; init; }

    /// <summary>The HTML email body.</summary>
    public required string Html { get; init; }

    /// <summary>The file attachments.</summary>
    public IReadOnlyList<WeMailAttachment>? Attachments { get; set; }

    /// <summary>Custom email headers.</summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }

    /// <summary>Tracking preference.</summary>
    public WeMailTracking Tracking { get; set; } = new();
}

internal sealed class WeMailAttachment
{
    /// <summary>The attachment file name.</summary>
    public required string Filename { get; init; }

    /// <summary>The attachment content encoded as Base64.</summary>
    public required string Content { get; init; }

    /// <summary>The MIME content type of the attachment.</summary>
    [JsonPropertyName("content_type")]
    public string? ContentType { get; init; }

    [JsonPropertyName("content_id")]
    public string? ContentId { get; init; }

    public string? Disposition { get; init; }
}

internal sealed class WeMailSendAcknowledgement
{
    /// <summary>
    /// Gets the Wemail message identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Gets the date and time when the message was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }
}

internal sealed class WeMailTracking {
    [JsonPropertyName("unsubscribe")]
    public bool Unsubscribe { get; set; } = true;
}