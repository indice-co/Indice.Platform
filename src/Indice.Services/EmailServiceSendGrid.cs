using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Indice.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Indice.Services;

/// <summary>
/// SendGrid implementation for the email service abstraction.
/// https://docs.sendgrid.com/api-reference/mail-send/mail-send
/// </summary>
public class EmailServiceSendGrid : IEmailService
{
    /// <summary>Represents the name of the SendGrid service as a constant string value.</summary>
    public const string ServiceName = "SendGrid";
    /// <summary>Creates a new instance of <see cref="EmailServiceSendGrid"/>.</summary>
    /// <param name="settings">An instance of <see cref="EmailServiceSendGridSettings"/> used to initialize the service.</param>
    /// <param name="httpClient">The HTTP client to use (DI managed)</param>
    /// <param name="htmlRenderingEngine">This is an abstraction for the rendering engine.</param>
    public EmailServiceSendGrid(
        IOptionsSnapshot<EmailServiceSendGridSettings> settings,
        HttpClient httpClient,
        IHtmlRenderingEngine htmlRenderingEngine) {
        Settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        Provider = new EmailProvider(ServiceName, new EmailSender(Settings.Sender!, Settings.SenderName));
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        HtmlRenderingEngine = htmlRenderingEngine ?? throw new ArgumentNullException(nameof(htmlRenderingEngine));
        if (HttpClient.BaseAddress == null) {
            HttpClient.BaseAddress = new Uri($"{Settings.Api.TrimEnd('/')}/");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(Settings.ApiKey);
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Settings.ApiKey);
    }

    private EmailServiceSendGridSettings Settings { get; }
    private HttpClient HttpClient { get; }
    /// <inheritdoc/>
    public IHtmlRenderingEngine HtmlRenderingEngine { get; }
    /// <inheritdoc/>
    public EmailProvider Provider { get; }

    /// <inheritdoc/>
    public async Task<SendReceipt> SendAsync(string[] recipients, string subject, string? body, EmailAttachment[]? attachments = null, EmailSender? from = null) {
        var bccRecipients = string.IsNullOrEmpty(Settings.BccRecipients)
            ? null
            : (Settings.BccRecipients ?? "").Split(';', ',', StringSplitOptions.RemoveEmptyEntries).Select(x => new SendGridEmailAddress { Email = x }).ToList();
        var request = new SendGridRequest {
            From = new SendGridEmailAddress {
                Email = from?.Address ?? Settings.Sender,
                Name = from?.DisplayName ?? Settings.SenderName
            },
            Subject = subject,
            Personalizations = new List<Personalizations> {
                new Personalizations {
                    To = recipients.Select(x => new SendGridEmailAddress {
                        Email = x
                    }).ToList(),
                    Bcc = bccRecipients
                }
            },
            Attachments = attachments is { Length: > 0 }
                ? attachments.Select(x => new SendGridAttachment {
                    Filename = x.FileName,
                    Type = FileExtensions.GetMimeType(Path.GetExtension(x.FileName)),
                    Content = Convert.ToBase64String(x.Data)
                }).ToList()
                : null,
            Content = new List<SendGridContent> {
                new () {
                    Value = body
                }
            }
        };

        var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var response = await HttpClient.PostAsync("mail/send", new StringContent(requestJson, Encoding.UTF8, MediaTypeNames.Application.Json));
        if (!response.IsSuccessStatusCode) {
            var content = await response.Content.ReadAsStringAsync();
            throw new EmailServiceException($"SendGrid service could not send email to recipients '{string.Join(", ", recipients)}'. Error is: '{content}'.");
        }
        var messageId = Guid.NewGuid().ToString();
        if (response.Headers.Contains("X-Message-ID")) {
            messageId = response.Headers.GetValues("X-Message-ID").First();
        }
        return new SendReceipt(messageId, DateTimeOffset.UtcNow);
    }
}

/// <inheritdoc/>
public sealed class SendGridWebhookHttpHandler(IOptions<EmailServiceSendGridSettings> options) : IWebhookHttpHandler
{
    private readonly EmailServiceSendGridSettings _options = options.Value;

    /// <inheritdoc/>
    public async Task<WebhookHttpResult> ParseAsync(IDictionary<string, StringValues> requestHeaders, IEnumerable<KeyValuePair<string, StringValues>> requestParameters, Stream requestBody, CancellationToken cancellationToken) {
        var payloadBytes = await ReadPayloadBytesAsync(requestBody, cancellationToken);
        if (!IsAuthorized(requestHeaders, payloadBytes)) {
            return WebhookHttpResult.ForbiddenRequest("Invalid SendGrid webhook signature.");
        }

        JsonNode? payload;
        try {
            payload = JsonNode.Parse(payloadBytes);
        } catch (JsonException) {
            return WebhookHttpResult.BadRequest("Invalid JSON payload");
        }

        if (payload is not JsonArray eventsPayload) {
            return WebhookHttpResult.BadRequest("Expected a JSON array of webhook events");
        }

        var events = ParseEvents(eventsPayload).ToList();
        return WebhookHttpResult.Ok(events, statusCode: 204);
    }

    private IEnumerable<WebhookEvent> ParseEvents(JsonArray payload) {
        foreach (var item in payload.OfType<JsonObject>()) {
            var messageId = GetString(item, "sg_message_id") ?? GetString(item, "smtp-id");
            if (string.IsNullOrWhiteSpace(messageId)) {
                continue;
            }

            var eventType = GetString(item, "event") ?? "unknown";
            var timestamp = GetTimestamp(item);
            var eventId = GetString(item, "sg_event_id") ?? $"{messageId}:{eventType}:{timestamp:O}";

            yield return new WebhookEvent {
                Provider = EmailServiceSendGrid.ServiceName,
                EventType = eventType,
                EventId = eventId,
                MessageId = messageId,
                Timestamp = timestamp,
                Recipient = GetString(item, "email") ?? string.Empty,
                Subject = GetString(item, "subject"),
                Payload = item,
                Succeeded = EventSucceeded(eventType)
            };
        }
    }
    private static bool EventSucceeded(string  eventType) {
        return eventType.Equals("delivered", StringComparison.OrdinalIgnoreCase) || eventType.Equals("sent", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsAuthorized(IDictionary<string, StringValues> requestHeaders, byte[] payloadBytes) {
        if (string.IsNullOrWhiteSpace(_options.WebhookVerificationKey)) {
            return true;
        }

        if (!requestHeaders.TryGetValue("x-twilio-email-event-webhook-signature", out var signatureHeaderValues)) {
            return false;
        }
        if (!requestHeaders.TryGetValue("x-twilio-email-event-webhook-timestamp", out var timestampHeaderValues)) {
            return false;
        }

        var signatureHeader = signatureHeaderValues.ToString();
        var timestamp = timestampHeaderValues.ToString();
        if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(timestamp)) {
            return false;
        }
        
        return VerifySignature(_options.WebhookVerificationKey!, signatureHeader, timestamp, payloadBytes);
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

    private static DateTimeOffset GetTimestamp(JsonObject data) {
        var rawTimestamp = GetString(data, "timestamp");
        if (long.TryParse(rawTimestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var epoch) && epoch > 0 && epoch <= DateTimeOffset.MaxValue.ToUnixTimeSeconds()) {
            return DateTimeOffset.FromUnixTimeSeconds(epoch);
        }

        return DateTimeOffset.UtcNow;
    }

    private static string? GetString(JsonObject data, string propertyName) {
        var value = data[propertyName];

        return value is JsonValue jsonValue &&
               jsonValue.TryGetValue<string>(out var text)
            ? text
            : value?.ToString();
    }

    /// <summary>Verifies the signature of a SendGrid webhook request.</summary>
    /// <param name="publicKey">The Base64-encoded public key.</param>
    /// <param name="signature">The Base64-encoded signature.</param>
    /// <param name="timestamp">The timestamp of the request.</param>
    /// <param name="rawBody">The raw body of the request.</param>
    /// <returns>True if the signature is valid; otherwise, false.</returns>
    public static bool VerifySignature(
        string publicKey,
        string signature,
        string timestamp,
        byte[] rawBody) {
        try {
            // Decode Base64 public key
            byte[] publicKeyBytes =
                Convert.FromBase64String(publicKey);

            // Import ECDSA public key
            using ECDsa ecdsa = ECDsa.Create();

            ecdsa.ImportSubjectPublicKeyInfo(
                publicKeyBytes,
                out _
            );

            // Timestamp + original raw JSON body
            byte[] timestampBytes =
                Encoding.UTF8.GetBytes(timestamp);

            byte[] payload = new byte[
                timestampBytes.Length + rawBody.Length
            ];

            Buffer.BlockCopy(
                timestampBytes, 0,
                payload, 0,
                timestampBytes.Length
            );

            Buffer.BlockCopy(
                rawBody, 0,
                payload, timestampBytes.Length,
                rawBody.Length
            );

            // Decode Base64 signature
            byte[] signatureBytes =
                Convert.FromBase64String(signature);

            // Verify ECDSA / SHA256
            return ecdsa.VerifyData(
                payload,
                signatureBytes,
                HashAlgorithmName.SHA256,
                DSASignatureFormat.Rfc3279DerSequence
            );
        } catch (CryptographicException) {
            return false;
        } catch (PlatformNotSupportedException) {
            return false;
        } catch (ArgumentException) {
            return false;
        } catch (FormatException) {
            return false;
        }
    }
}

/// <summary>Custom settings that are used to send emails via SendGrid.</summary>
public class EmailServiceSendGridSettings
{
    /// <summary>The configuration section name.</summary>
    public const string Name = "SendGrid";
    /// <summary>The default sender address (ex. no-reply@indice.gr).</summary>
    public string Sender { get; set; } = null!;
    /// <summary>The default sender name (ex. INDICE SA)</summary>
    public string? SenderName { get; set; }
    /// <summary>Optional email addresses that are always added as blind carbon copy recipients.</summary>
    public string? BccRecipients { get; set; }
    /// <summary>The SendGrid API key.</summary>
    public string? ApiKey { get; set; }
    /// <summary>The SendGrid API URL (ex. https://api.sendgrid.com/v3/).</summary>
    public string Api { get; set; } = "https://api.sendgrid.com/v3/";
    /// <summary>
    /// This is the Webhook verification key from SendGrid's Event Webhook settings.
    /// </summary>
    public string? WebhookVerificationKey { get; set; }
}

#region SendGrid models

internal class SendGridRequest
{
    public SendGridEmailAddress? From { get; set; }
    public string? Subject { get; set; }
    [JsonPropertyName("template_id")]
    public string? TemplateId { get; set; }
    public List<Personalizations>? Personalizations { get; set; }
    public List<SendGridAttachment>? Attachments { get; set; }
    public List<SendGridContent>? Content { get; set; }
}

internal class SendGridEmailAddress
{
    public string? Email { get; set; }
    public string? Name { get; set; }
}

internal class SendGridAttachment
{
    /// <summary>The Base64 encoded content of the attachment.</summary>
    public string? Content { get; set; }
    /// <summary>The attachment's filename.</summary>
    public string? Filename { get; set; }
    /// <summary>The MIME type of the content you are attaching.</summary>
    public string? Type { get; set; }
    /// <summary>Allowed Values: inline, attachment</summary>
    public string Disposition { get; set; } = "attachment";
}

internal class Personalizations
{
    public IEnumerable<SendGridEmailAddress>? To { get; set; }
    public IEnumerable<SendGridEmailAddress>? Cc { get; set; }
    public IEnumerable<SendGridEmailAddress>? Bcc { get; set; }
    [JsonPropertyName("dynamic_template_data")]
    public object? TemplateData { get; set; }
}

internal class SendGridContent
{
    /// <summary>The mime type of the content you are including in your email. For example, text/plain or text/html.</summary>
    public string Type { get; set; } = "text/html";
    /// <summary>The actual content of the specified mime type that you are including in your email.</summary>
    public string? Value { get; set; }
}

#endregion