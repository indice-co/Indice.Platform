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
        if (hasUsername != hasPassword) {
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

        var tolerance = _settings.WebhookReplayTolerance > TimeSpan.Zero
            ? _settings.WebhookReplayTolerance
            : TimeSpan.FromMinutes(5);

        if (IsHmacAuthorized(payloadBytes, signatureHeader, _settings.WebhookSigningSecret!, tolerance)) {
            return true;
        }

        if (!TryParseSignatureHeader(signatureHeader, out var timestampRaw, out var signatures, out _)) {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(timestampRaw)) {
            return false;
        }

        var timestampHeaderName = string.IsNullOrWhiteSpace(_settings.WebhookTimestampHeaderName)
            ? "X-Wemail-Timestamp"
            : _settings.WebhookTimestampHeaderName;
        if (!TryGetHeaderValue(requestHeaders, timestampHeaderName, out var timestampHeaderValue) || string.IsNullOrWhiteSpace(timestampHeaderValue)) {
            return false;
        }

        var syntheticHeader = string.Join(',', signatures.Select(signature => $"v1={signature}"));
        syntheticHeader = $"t={timestampHeaderValue},{syntheticHeader}";
        return IsHmacAuthorized(payloadBytes, syntheticHeader, _settings.WebhookSigningSecret!, tolerance);
    }

    public static bool IsHmacAuthorized(
        byte[] rawBody,
        string? signatureHeader,
        string secret,
        TimeSpan? tolerance = null) {
        if (rawBody is null || rawBody.Length == 0 || string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(secret)) {
            return false;
        }

        if (!TryParseSignatureHeader(signatureHeader, out var timestampRaw, out var signatures, out _)) {
            return false;
        }

        if (string.IsNullOrWhiteSpace(timestampRaw) || !TryParseTimestamp(timestampRaw, out var timestamp)) {
            return false;
        }

        var maxAge = tolerance ?? TimeSpan.FromMinutes(5);
        if ((DateTimeOffset.UtcNow - timestamp).Duration() > maxAge) {
            return false;
        }

        var payloadCandidates = BuildSigningPayloadCandidates(rawBody, timestampRaw.Trim(), timestamp);
        var secretCandidates = BuildSecretCandidates(secret).ToList();

        foreach (var signatureValue in signatures) {
            if (!TryDecodeSignatureToBytes(signatureValue, out var providedSignature)) {
                continue;
            }

            foreach (var payload in payloadCandidates) {
                foreach (var secretCandidate in secretCandidates) {
                    foreach (var digest in BuildDigests(secretCandidate, payload)) {
                        if (CryptographicOperations.FixedTimeEquals(digest, providedSignature)) {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private static IEnumerable<byte[]> BuildSigningPayloadCandidates(byte[] payloadBytes, string timestampRaw, DateTimeOffset timestamp) {
        var rawTimestampBytes = Encoding.UTF8.GetBytes(timestampRaw);
        var timestampSecondsBytes = Encoding.UTF8.GetBytes(timestamp.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        var timestampMillisecondsBytes = Encoding.UTF8.GetBytes(timestamp.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture));
        var dot = new byte[] { (byte)'.' };
        var newline = new byte[] { (byte)'\n' };

        foreach (var ts in new[] { rawTimestampBytes, timestampSecondsBytes, timestampMillisecondsBytes }) {
            yield return BuildSignedPayload(ts, payloadBytes, dot);
            yield return BuildSignedPayload(ts, payloadBytes, newline);
            yield return BuildSignedPayload(ts, payloadBytes, []);
            yield return BuildSignedPayload(payloadBytes, ts, dot);
            yield return BuildSignedPayload(payloadBytes, ts, newline);
            yield return BuildSignedPayload(payloadBytes, ts, []);
        }
    }

    private static IEnumerable<byte[]> BuildSecretCandidates(string secret) {
        yield return Encoding.UTF8.GetBytes(secret);

        const string Prefix = "whsec_";
        if (!secret.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) {
            yield break;
        }

        var suffix = secret[Prefix.Length..];
        if (string.IsNullOrWhiteSpace(suffix)) {
            yield break;
        }

        yield return Encoding.UTF8.GetBytes(suffix);
        if (TryDecodeSignatureToBytes(suffix, out var decoded)) {
            yield return decoded;
        }
    }

    private static IEnumerable<byte[]> BuildDigests(byte[] secretBytes, byte[] payloadBytes) {
        yield return HMACSHA256.HashData(secretBytes, payloadBytes);
        yield return HMACSHA512.HashData(secretBytes, payloadBytes);
    }

    private static byte[] BuildSignedPayload(byte[] left, byte[] right, byte[] separator) {
        var payload = new byte[left.Length + separator.Length + right.Length];
        Buffer.BlockCopy(left, 0, payload, 0, left.Length);
        if (separator.Length > 0) {
            Buffer.BlockCopy(separator, 0, payload, left.Length, separator.Length);
        }
        Buffer.BlockCopy(right, 0, payload, left.Length + separator.Length, right.Length);
        return payload;
    }

    private static bool TryParseSignatureHeader(string signatureHeader, out string? timestamp, out List<string> signatures, out string? keyId) {
        timestamp = null;
        keyId = null;
        signatures = [];

        var segments = signatureHeader.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments) {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex >= segment.Length) {
                continue;
            }

            var key = segment[..separatorIndex].Trim();
            var value = NormalizeSignatureValue(segment[(separatorIndex + 1)..].Trim());
            if (string.IsNullOrWhiteSpace(value)) {
                continue;
            }

            if (key.Equals("t", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("ts", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("timestamp", StringComparison.OrdinalIgnoreCase)) {
                timestamp = value;
                continue;
            }

            if (key.Equals("kid", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("keyid", StringComparison.OrdinalIgnoreCase)) {
                keyId = value;
                continue;
            }

            if (key.Equals("v1", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("sig", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("signature", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("hmac", StringComparison.OrdinalIgnoreCase)) {
                signatures.Add(value);
            }
        }

        if (signatures.Count == 0) {
            var fallback = NormalizeSignatureValue(signatureHeader);
            if (!string.IsNullOrWhiteSpace(fallback)) {
                signatures.Add(fallback);
            }
        }

        return signatures.Count > 0;
    }

    private static string NormalizeSignatureValue(string value) {
        var normalized = value.Trim();
        if (normalized.Length >= 2 &&
            ((normalized[0] == '"' && normalized[^1] == '"') ||
             (normalized[0] == '\'' && normalized[^1] == '\''))) {
            normalized = normalized[1..^1];
        }

        const string Prefix = "sha256=";
        if (normalized.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) {
            normalized = normalized[Prefix.Length..];
        }

        return normalized;
    }

    private static bool TryDecodeSignatureToBytes(string signature, out byte[] bytes) {
        bytes = [];
        if (string.IsNullOrWhiteSpace(signature)) {
            return false;
        }

        var normalized = NormalizeSignatureValue(signature);
        if (normalized.Length % 2 == 0 && normalized.All(Uri.IsHexDigit)) {
            try {
                bytes = Convert.FromHexString(normalized);
                return true;
            } catch (FormatException) {
            }
        }

        if (TryDecodeBase64(normalized, out bytes)) {
            return true;
        }

        var base64Url = normalized.Replace('-', '+').Replace('_', '/');
        var padding = base64Url.Length % 4;
        if (padding != 0) {
            base64Url = base64Url.PadRight(base64Url.Length + (4 - padding), '=');
        }

        return TryDecodeBase64(base64Url, out bytes);
    }

    private static bool TryDecodeBase64(string value, out byte[] bytes) {
        try {
            bytes = Convert.FromBase64String(value);
            return true;
        } catch (FormatException) {
            bytes = [];
            return false;
        }
    }

    private static bool TryParseTimestamp(string timestampRaw, out DateTimeOffset timestamp) {
        if (long.TryParse(timestampRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var epoch) && epoch > 0) {
            timestamp = epoch > 9_999_999_999
                ? DateTimeOffset.FromUnixTimeMilliseconds(epoch)
                : DateTimeOffset.FromUnixTimeSeconds(epoch);
            return true;
        }

        return DateTimeOffset.TryParse(timestampRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out timestamp);
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

    private static IEnumerable<WebhookEvent> ParseEvents(JsonNode payload) {
        var items = payload switch {
            JsonObject item => new[] { item },
            JsonArray array => array.OfType<JsonObject>(),
            _ => Enumerable.Empty<JsonObject>()
        };

        foreach (var item in items) {
            var messageId = GetString(item, "message_id")
                            ?? GetString(item, "messageId")
                            ?? GetString(item, "id")
                            ?? GetString(item, "data.message_id")
                            ?? GetString(item, "data.id");

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

        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var epoch) && epoch > 0) {
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