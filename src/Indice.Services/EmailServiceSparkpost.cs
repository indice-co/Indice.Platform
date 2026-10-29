using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Indice.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Indice.Services;

/// <summary>
/// SparkPost implementation for the email service abstraction.
/// https://developers.sparkpost.com/api/transmissions.html
/// </summary>
public class EmailServiceSparkPost : IEmailService
{
    /// <summary>Represents the name of the SparkPost service as a constant string value.</summary>
    public const string ServiceName = "SparkPost";

    /// <summary>Creates a new instance of <see cref="EmailServiceSparkPost"/>.</summary>
    /// <param name="settings">An instance of <see cref="EmailServiceSparkPostSettings"/> used to initialize the service.</param>
    /// <param name="httpClient">The HTTP client to use (DI managed)</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <param name="htmlRenderingEngine">This is an abstraction for the rendering engine.</param>
    public EmailServiceSparkPost(
        IOptionsSnapshot<EmailServiceSparkPostSettings> settings,
        HttpClient httpClient,
        ILogger<EmailServiceSparkPost> logger,
        IHtmlRenderingEngine htmlRenderingEngine
    ) {
        Settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        Provider = new EmailProvider(ServiceName, new EmailSender(Settings.Sender, Settings.SenderName));
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        HtmlRenderingEngine = htmlRenderingEngine ?? throw new ArgumentNullException(nameof(htmlRenderingEngine));
        if (HttpClient.BaseAddress == null) {
            HttpClient.BaseAddress = new Uri(Settings.Api.TrimEnd('/') + "/");
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Settings.ApiKey!);
        }
    }

    private EmailServiceSparkPostSettings Settings { get; }
    private HttpClient HttpClient { get; }
    private ILogger<EmailServiceSparkPost> Logger { get; }
    /// <inheritdoc/>
    public IHtmlRenderingEngine HtmlRenderingEngine { get; }
    /// <inheritdoc/>
    public EmailProvider Provider { get; }

    /// <inheritdoc/>
    public async Task<SendReceipt> SendAsync(string[] recipients, string subject, string? body, EmailAttachment[]? attachments = null, EmailSender? from = null) {
        var messageId = Guid.NewGuid().ToString();
        var bccRecipients = (Settings.BccRecipients ?? "").Split(';', ',');
        var recipientAddresses = recipients.Select(recipient => new SparkPostRecipient {
            Address = new SparkPostRecipientEmailAddress {
                Email = recipient
            }
        });
        var bccAddresses = bccRecipients.SelectMany(bcc => recipients.Select(recipient => new SparkPostRecipient {
            Address = new SparkPostRecipientEmailAddress {
                Email = bcc,
                HeaderTo = recipient
            }
        }));
        var request = new SparkPostRequest {
            Content = new SparkPostContent {
                From = new SparkPostSenderAddress {
                    Email = from?.Address ?? Settings.Sender,
                    Name = from?.DisplayName ?? Settings.SenderName
                },
                Subject = subject,
                Html = body
            },
            Recipients = recipientAddresses.Concat(bccAddresses).ToArray()
        };
        if (attachments?.Length > 0) {
            var attachmentsList = new List<SparkPostAttachment>();
            foreach (var attachment in attachments) {
                attachmentsList.Add(new SparkPostAttachment {
                    Name = attachment.FileName,
                    Type = FileExtensions.GetMimeType(Path.GetExtension(attachment.FileName)),
                    Data = Convert.ToBase64String(attachment.Data)
                });
            }
            request.Content.Attachments = [.. attachmentsList];
        }
        var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        var response = await HttpClient.PostAsync("transmissions", new StringContent(requestJson, Encoding.UTF8, MediaTypeNames.Application.Json));
        if (!response.IsSuccessStatusCode) {
            var content = await response.Content.ReadAsStringAsync();
            Logger.LogError("SparkPost service could not send email to recipients '{recipients}'. Error is: '{content}'.", string.Join(", ", recipients), content);
            throw new EmailServiceException($"SparkPost service could not send email to recipients '{string.Join(", ", recipients)}'. Error is: '{content}'.");
        }
        var responseJson = await response.Content.ReadAsStringAsync();
        var sparkPostResponse = JsonSerializer.Deserialize<SparkPostTransmissionResponse>(responseJson)!;
        return new SendReceipt(sparkPostResponse.Results?.Id ?? messageId, DateTimeOffset.UtcNow);
    }
}

/// <inheritdoc/>
public sealed class SparkPostWebhookHttpHandler(IOptions<EmailServiceSparkPostSettings> options) : IWebhookHttpHandler
{
    private readonly EmailServiceSparkPostSettings _options = options.Value;

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

        if (payload is not JsonArray) {
            return WebhookHttpResult.BadRequest("Expected a JSON array of webhook events");
        }

        var events = ParseEvents(payload).ToList();

        return WebhookHttpResult.Ok(events);
    }

    private static IEnumerable<WebhookEvent> ParseEvents(JsonNode? root) {
        if (root is not JsonArray items)
            yield break;

        var validItems = items
            .Select(item => item is JsonObject envelope && envelope["msys"] is JsonObject msys
                ? (Envelope: envelope, Msys: msys)
                : (Envelope: null, Msys: null))
            .Where(entry => entry.Envelope is not null && entry.Msys is not null);

        foreach (var entry in validItems) {
            foreach (var family in entry.Msys!) {
                var familyEvents = family.Value switch {
                    JsonObject obj => new[] { obj },
                    JsonArray array => array.OfType<JsonObject>(),
                    _ => Enumerable.Empty<JsonObject>()
                };

                foreach (var data in familyEvents) {
                    var eventId = GetString(data, "event_id");
                    var messageId = GetString(data, "message_id");

                    if (string.IsNullOrWhiteSpace(eventId) ||
                        string.IsNullOrWhiteSpace(messageId))
                        continue;

                    yield return new WebhookEvent {
                        Provider = EmailServiceSparkPost.ServiceName,
                        EventType = GetString(data, "type") ?? family.Key,
                        EventId = eventId,
                        MessageId = messageId,
                        Timestamp = GetTimestamp(data),
                        Recipient = GetString(data, "rcpt_to")
                            ?? GetString(data, "raw_rcpt_to")
                            ?? GetString(data, "sms_dst")
                            ?? string.Empty,
                        Subject = GetString(data, "subject")
                    };
                }
            }
        }
    }

    private static string? GetString(JsonObject data, string propertyName) {
        var value = data[propertyName];

        return value is JsonValue jsonValue &&
               jsonValue.TryGetValue<string>(out var text)
            ? text
            : value?.ToString();
    }

    private static DateTimeOffset GetTimestamp(JsonObject data) {
        var timestampMs = GetString(data, "timestamp_ms");

        if (long.TryParse(timestampMs, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var milliseconds) &&
            milliseconds > 0)
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);

        var timestamp = GetString(data, "timestamp");

        if (long.TryParse(timestamp, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var seconds) &&
            seconds > 0)
            return DateTimeOffset.FromUnixTimeSeconds(seconds);

        var injectionTime = GetString(data, "injection_time");

        if (DateTimeOffset.TryParse(
                injectionTime,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var parsed))
            return parsed;

        return DateTimeOffset.UtcNow;
    }

    private bool IsAuthorized(IDictionary<string, StringValues> requestHeaders) {
        var hasUsername = !string.IsNullOrWhiteSpace(_options.WebhookUsername);
        var hasPassword = !string.IsNullOrWhiteSpace(_options.WebhookPassword);
        bool incompleteCredentials = hasUsername != hasPassword;
        bool noAuth = !incompleteCredentials && !hasUsername;

        if (incompleteCredentials) {
            return false;
        }

        if (noAuth) {
            return true;
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

        var username = Encoding.UTF8.GetBytes(_options.WebhookUsername!);
        var password = Encoding.UTF8.GetBytes(_options.WebhookPassword!);

        var usernameMatches = CryptographicOperations.FixedTimeEquals(credentials.AsSpan(0, separator), username);
        var passwordMatches = CryptographicOperations.FixedTimeEquals(credentials.AsSpan(separator + 1), password);

        return usernameMatches & passwordMatches;
    }
}

/// <summary>Custom settings that are used to send emails via SparkPost.</summary>
public class EmailServiceSparkPostSettings
{
    /// <summary>The configuration section name.</summary>
    public const string Name = "SparkPost";
    /// <summary>The default sender address (ex. no-reply@indice.gr).</summary>
    public string Sender { get; set; } = null!;
    /// <summary>The default sender name (ex. INDICE SA)</summary>
    public string? SenderName { get; set; }
    /// <summary>Optional email addresses that are always added as blind carbon copy recipients.</summary>
    public string? BccRecipients { get; set; }
    /// <summary>The SparkPost API key.</summary>
    public string? ApiKey { get; set; }
    /// <summary>The SparkPost API URL (ex. https://api.eu.sparkpost.com/api/v1/).</summary>
    public string Api { get; set; } = "https://api.eu.sparkpost.com/api/v1/";
    /// <summary>The username used to authenticate incoming SparkPost webhook requests.</summary>
    public string? WebhookUsername { get; set; }
    /// <summary>The password used to authenticate incoming SparkPost webhook requests.</summary>
    public string? WebhookPassword { get; set; }
}

#region SparkPost Models
internal class SparkPostRequest
{
    public SparkPostContent? Content { get; set; }
    public SparkPostRecipient[]? Recipients { get; set; }
}

internal class SparkPostContent
{
    public SparkPostSenderAddress? From { get; set; }
    public string? Subject { get; set; }
    public string? Html { get; set; }
    public SparkPostAttachment[]? Attachments { get; set; }
}

internal class SparkPostSenderAddress
{
    public string? Email { get; set; }
    public string? Name { get; set; }
}

internal class SparkPostRecipient
{
    public SparkPostRecipientEmailAddress? Address { get; set; }
}

internal class SparkPostRecipientEmailAddress
{
    public string? Email { get; set; }
    /// <summary>
    /// Decides whether this email address will be associated with an other one. 
    /// If left blank the address will receive a separate email.
    /// </summary>
    [JsonPropertyName("header_to")]
    public string? HeaderTo { get; set; }
}

internal class SparkPostAttachment
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Data { get; set; }
}

internal class SparkPostTransmissionResponse
{
    [JsonPropertyName("results")]
    public SparkPostTransmissionResults Results { get; set; } = new SparkPostTransmissionResults();
}

internal class SparkPostTransmissionResults
{
    [JsonPropertyName("total_rejected_recipients")]
    public int TotalRejectedRecipients { get; set; }

    [JsonPropertyName("total_accepted_recipients")]
    public int TotalAcceptedRecipients { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;
}
#endregion