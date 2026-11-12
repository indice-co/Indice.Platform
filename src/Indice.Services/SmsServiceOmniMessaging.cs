using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Indice.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Indice.Services;

/// <summary>
/// Viber/SMS service implementation using the Omni messaging service.
/// </summary>
public sealed class SmsServiceOmniMessaging : ISmsService
{
    /// <summary>
    /// The Omni messaging base URI address.
    /// </summary>
    internal const string BASE_URI = "https://rest.omni-messaging.com";

    /// <summary>
    /// The Omni messaging service endpoint.
    /// </summary>
    internal const string SERVICE_ENDPOINT = "/campaign/v1/{0}/campaigns";

    /// <summary>
    /// Gets the settings required to configure the service.
    /// </summary>
    internal SmsServiceOmniMessagingSettings Options { get; }

    /// <summary>
    /// Gets the HTTP client used to communicate with the Omni messaging service.
    /// </summary>
    internal HttpClient HttpClient { get; }

    /// <summary>
    /// Gets the logger used by the service.
    /// </summary>
    internal ILogger<SmsServiceOmniMessaging> Logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SmsServiceOmniMessaging"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to communicate with the Omni messaging service.</param>
    /// <param name="options">The Omni messaging service settings.</param>
    /// <param name="logger">The logger used by the service.</param>
    public SmsServiceOmniMessaging(
        HttpClient httpClient,
        IOptionsSnapshot<SmsServiceOmniMessagingSettings> options,
        ILogger<SmsServiceOmniMessaging> logger) {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(Options.ApiKey)) {
            throw new ArgumentException($"SMS settings {nameof(SmsServiceOmniMessagingSettings.ApiKey)} is empty.");
        }

        if (string.IsNullOrWhiteSpace(Options.AccountId)) {
            throw new ArgumentException($"SMS settings {nameof(SmsServiceOmniMessagingSettings.AccountId)} is empty.");
        }

        if (string.IsNullOrWhiteSpace(Options.Sender)) {
            throw new ArgumentException($"SMS settings {nameof(SmsServiceOmniMessagingSettings.Sender)} is empty.");
        }

        if (Options.ViberValidity < 1) {
            throw new ArgumentException($"SMS settings {nameof(SmsServiceOmniMessagingSettings.ViberValidity)} must be greater than zero.");
        }

        if (Options.SmsValidity < 1) {
            throw new ArgumentException($"SMS settings {nameof(SmsServiceOmniMessagingSettings.SmsValidity)} must be greater than zero.");
        }
    }

    /// <inheritdoc/>
    public async Task<SendReceipt> SendAsync(string destination, string subject, string? body, SmsSender? sender = null) {
        var recipients = (destination ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (recipients.Length == 0) {
            throw new ArgumentException("Recipients list cannot be empty.", nameof(destination));
        }

        recipients = recipients.Select(recipient => {
            if (!PhoneNumber.TryParse(recipient, out var phone)) {
                throw new ArgumentException(
                    "Invalid recipients. Recipients should be valid phone numbers.",
                    nameof(destination));
            }

            return phone.ToString("D");
        }).ToArray();

        if (recipients.Any(phoneNumber => phoneNumber.Any(numberChar => !char.IsNumber(numberChar)))) {
            throw new ArgumentException(
                "Invalid recipients. Recipients cannot contain letters.",
                nameof(destination));
        }

        if (string.IsNullOrWhiteSpace(body)) {
            throw new ArgumentException("Message body cannot be empty.", nameof(body));
        }

        var senderId = sender?.Id ?? Options.Sender;

        var payload = SmsServiceOmniRequest.Create(
            senderId!,
            recipients,
            body,
            Options.Channel,
            Options.ViberValidity,
            Options.SmsValidity,
            Options.UseUtf8);

        using var request = new HttpRequestMessage {
            Method = HttpMethod.Post,
            RequestUri = new Uri(
                $"{HttpClient.BaseAddress ?? new Uri(BASE_URI)}{string.Format(SERVICE_ENDPOINT, Options.AccountId)}"),
            Content = new StringContent(
                payload.ToJson(),
                Encoding.UTF8,
                MediaTypeNames.Application.Json)
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        request.Headers.Add("API-Token", Options.ApiKey);

        try {
            using var httpResponse = await HttpClient.SendAsync(request);
            var responseString = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode) {
                Logger.LogWarning(
                    "Viber/SMS Delivery failed. {StatusCode}: {ResponseString}",
                    httpResponse.StatusCode,
                    responseString);

                throw new SmsServiceException(
                    $"Viber/SMS Delivery failed. {httpResponse.StatusCode}: {responseString}");
            }

            var response = JsonSerializer.Deserialize<SmsServiceOmniResponse>(
                responseString,
                GetJsonSerializerOptions());

            var campaignId = response?.GetCampaignId();

            Logger.LogInformation(
                "Viber/SMS campaign successfully submitted to Omni: {CampaignId}",
                campaignId);

            return new SendReceipt(
                campaignId ?? string.Empty,
                DateTimeOffset.UtcNow);
        } catch (SmsServiceException) {
            throw;
        } catch (OperationCanceledException ex) {
            Logger.LogError(ex, "Viber/SMS Delivery took too long.");
            throw new SmsServiceException("Viber/SMS Delivery took too long.", ex);
        } catch (HttpRequestException ex) {
            Logger.LogError(ex, "Viber/SMS Delivery failed.");
            throw new SmsServiceException("Viber/SMS Delivery failed.", ex);
        }
    }

    /// <summary>
    /// Determines whether this service supports the specified delivery channel.
    /// </summary>
    /// <param name="deliveryChannel">The delivery channel to check.</param>
    /// <returns>
    /// <see langword="true"/> if the delivery channel is supported;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Supports(string deliveryChannel) =>
        Options.Channel switch {
            SmsServiceOmniMessagingMode.SmsOnly => "SMS".Equals(deliveryChannel, StringComparison.OrdinalIgnoreCase),
            SmsServiceOmniMessagingMode.ViberOnly => "Viber".Equals(deliveryChannel, StringComparison.OrdinalIgnoreCase),
            SmsServiceOmniMessagingMode.ViberWithSmsFallback =>
                "Viber".Equals(deliveryChannel, StringComparison.OrdinalIgnoreCase) ||
                "SMS".Equals(deliveryChannel, StringComparison.OrdinalIgnoreCase),
            _ => false
        };

    /// <summary>
    /// Gets the JSON serializer options used for Omni requests and responses.
    /// </summary>
    /// <returns>The configured JSON serializer options.</returns>
    internal static JsonSerializerOptions GetJsonSerializerOptions() => new() {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

/// <summary>
/// Specifies the delivery mode used by the Omni messaging service.
/// </summary>
public enum SmsServiceOmniMessagingMode
{
    /// <summary>
    /// Sends the message using SMS only.
    /// </summary>
    SmsOnly,

    /// <summary>
    /// Sends the message using Viber only.
    /// </summary>
    ViberOnly,

    /// <summary>
    /// Sends the message using Viber with SMS fallback.
    /// </summary>
    ViberWithSmsFallback
}

/// <summary>
/// Specifies the webhook authentication method used by Omni messaging callbacks.
/// </summary>
public enum SmsServiceOmniMessagingWebhookAuthenticationMethod
{
    /// <summary>
    /// Validates an API key header against the configured secret.
    /// </summary>
    ApiKey,

    /// <summary>
    /// Validates HTTP Basic credentials from the Authorization header.
    /// </summary>
    Basic
}

/// <summary>
/// Represents the configuration settings for the Omni Viber/SMS service.
/// </summary>
public sealed class SmsServiceOmniMessagingSettings
{
    /// <summary>
    /// Gets or sets the Omni API token.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the Omni account identifier.
    /// </summary>
    public string? AccountId { get; set; }

    /// <summary>
    /// Gets or sets the sender identifier used for Viber and SMS messages.
    /// </summary>
    public string? Sender { get; set; }

    /// <summary>
    /// Gets or sets the primary messaging channel preference.
    /// </summary>
    public SmsServiceOmniMessagingMode Channel { get; set; } = SmsServiceOmniMessagingMode.SmsOnly;

    /// <summary>
    /// Gets or sets the Viber message validity period in seconds.
    /// </summary>
    public int ViberValidity { get; set; } = 3600;

    /// <summary>
    /// Gets or sets the SMS message validity period in seconds.
    /// </summary>
    public int SmsValidity { get; set; } = 300;

    /// <summary>
    /// Gets or sets messages supported character set (GSM or UTF-8).
    /// </summary>
    public bool UseUtf8 { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional shared secret used to validate incoming Omni webhooks.
    /// If empty, webhook authentication validation is skipped.
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// Gets or sets the webhook authentication method configured in Omni.
    /// </summary>
    public SmsServiceOmniMessagingWebhookAuthenticationMethod WebhookAuthenticationMethod { get; set; } = SmsServiceOmniMessagingWebhookAuthenticationMethod.ApiKey;

    /// <summary>
    /// Gets or sets the API key header name used when <see cref="WebhookAuthenticationMethod"/> is <see cref="SmsServiceOmniMessagingWebhookAuthenticationMethod.ApiKey"/>.
    /// </summary>
    public string? WebhookApiKeyHeaderName { get; set; } = "X-API-Key";

    /// <summary>
    /// Gets or sets the webhook username used when <see cref="WebhookAuthenticationMethod"/> is <see cref="SmsServiceOmniMessagingWebhookAuthenticationMethod.Basic"/>.
    /// </summary>
    public string? WebhookUsername { get; set; }

    /// <summary>
    /// Gets or sets the webhook password used when <see cref="WebhookAuthenticationMethod"/> is <see cref="SmsServiceOmniMessagingWebhookAuthenticationMethod.Basic"/>.
    /// </summary>
    public string? WebhookPassword { get; set; }
}

/// <summary>
/// Represents a request sent to the Omni campaign API.
/// </summary>
internal sealed class SmsServiceOmniRequest
{
    /// <summary>
    /// Gets or sets the campaign name.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the campaign recipients.
    /// </summary>
    public List<OmniRecipient> Recipients { get; set; } = [];

    /// <summary>
    /// Gets or sets the campaign content.
    /// </summary>
    public OmniContent Content { get; set; } = null!;

    /// <summary>
    /// Gets or sets the campaign scheduling configuration.
    /// </summary>
    public OmniScheduling Scheduling { get; set; } = null!;

    /// <summary>
    /// Creates an Omni campaign request.
    /// </summary>
    public static SmsServiceOmniRequest Create(
    string sender,
    string[] recipients,
    string message,
    SmsServiceOmniMessagingMode mode,
    int viberValidity,
    int smsValidity,
    bool useUtf8) {
        var useSms = mode is SmsServiceOmniMessagingMode.SmsOnly or SmsServiceOmniMessagingMode.ViberWithSmsFallback;
        var useViber = mode is SmsServiceOmniMessagingMode.ViberOnly or SmsServiceOmniMessagingMode.ViberWithSmsFallback;
        var useFallback = mode is SmsServiceOmniMessagingMode.ViberWithSmsFallback;

        return new SmsServiceOmniRequest {
            Name = sender,
            Recipients = recipients
                .Select(phoneNumber => new OmniRecipient {
                    UniqueId = phoneNumber,
                    Mobile = phoneNumber
                })
                .ToList(),
            Content = new OmniContent {
                Sms = useSms
                    ? new OmniSms {
                        From = sender,
                        Text = message,
                        Charset = useUtf8 ? "UTF-8" : "GSM"
                    }
                    : null,
                Viber = useViber
                    ? new OmniViber {
                        Message = new OmniViberMessage {
                            Text = message
                        }
                    }
                    : null
            },
            Scheduling = new OmniScheduling {
                Channels = new OmniSchedulingChannels {
                    Sms = useSms
                        ? new OmniChannelScheduling {
                            TimePeriod = smsValidity
                        }
                        : null,
                    Viber = useViber
                        ? new OmniChannelScheduling {
                            TimePeriod = viberValidity
                        }
                        : null
                },
                Fallback = useFallback
                    ? ["viber", "sms"]
                    : null
            }
        };
    }

    /// <summary>
    /// Serializes the request to JSON.
    /// </summary>
    /// <returns>The serialized JSON representation of the request.</returns>
    public string ToJson()
        => JsonSerializer.Serialize(this, GetJsonSerializerOptions());

    private static JsonSerializerOptions GetJsonSerializerOptions() => new() {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

internal sealed class OmniCampaignRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("recipients")]
    public required IReadOnlyCollection<OmniRecipient> Recipients { get; init; }

    [JsonPropertyName("content")]
    public required OmniContent Content { get; init; }

    [JsonPropertyName("scheduling")]
    public required OmniScheduling Scheduling { get; init; }
}

internal sealed class OmniRecipient
{
    [JsonPropertyName("unique_id")]
    public required string UniqueId { get; init; }

    [JsonPropertyName("mobile")]
    public required string Mobile { get; init; }
}

internal sealed class OmniContent
{
    [JsonPropertyName("sms")]
    public OmniSms? Sms { get; set; }

    [JsonPropertyName("viber")]
    public OmniViber? Viber { get; set; }
}

internal sealed class OmniSms
{
    [JsonPropertyName("from")]
    public required string From { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("charset")]
    public required string Charset { get; init; }
}

internal sealed class OmniViber
{
    [JsonPropertyName("type")]
    public int Type { get; init; } = 106;

    [JsonPropertyName("message")]
    public required OmniViberMessage Message { get; init; }
}

internal sealed class OmniViberMessage
{
    [JsonPropertyName("text")]
    public required string Text { get; init; }
}

internal sealed class OmniScheduling
{
    [JsonPropertyName("channels")]
    public required OmniSchedulingChannels Channels { get; init; }

    [JsonPropertyName("fallback")]
    public IReadOnlyCollection<string>? Fallback { get; init; }
}

internal sealed class OmniSchedulingChannels
{
    [JsonPropertyName("viber")]
    public OmniChannelScheduling? Viber { get; init; }

    [JsonPropertyName("sms")]
    public OmniChannelScheduling? Sms { get; init; }
}

internal sealed class OmniChannelScheduling
{
    [JsonPropertyName("time_period")]
    public int TimePeriod { get; init; }
}

internal sealed class SmsServiceOmniResponse
{
    public string? Id { get; set; }

    [JsonPropertyName("campaign_id")]
    public string? CampaignId { get; set; }

    [JsonPropertyName("request_id")]
    public long? RequestId { get; set; }

    public string? GetCampaignId() {
        if (!string.IsNullOrWhiteSpace(Id)) {
            return Id;
        }

        if (!string.IsNullOrWhiteSpace(CampaignId)) {
            return CampaignId;
        }

        return RequestId?.ToString();
    }
}

/// <inheritdoc/>
public sealed class OmnimessagingWebhookHttpHandler : IWebhookHttpHandler
{
    private readonly SmsServiceOmniMessagingSettings _settings;

    /// <inheritdoc/>
    public OmnimessagingWebhookHttpHandler(IOptionsSnapshot<SmsServiceOmniMessagingSettings> settings) {
        _settings = settings.Value;
    }

    /// <inheritdoc/>
    public async Task<WebhookHttpResult> ParseAsync(
        IDictionary<string, StringValues> requestHeaders,
        IEnumerable<KeyValuePair<string, StringValues>> requestParameters,
        Stream requestBody,
        CancellationToken cancellationToken) {
        if (!IsAuthorized(requestHeaders)) {
            return WebhookHttpResult.BadRequest("Invalid OmniMessaging webhook authentication.");
        }

        using var reader = new StreamReader(requestBody);
        var bodyString = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(bodyString)) {
            return WebhookHttpResult.BadRequest("OmniMessaging webhook body cannot be empty.");
        }

        JsonDocument document;
        try {
            document = JsonDocument.Parse(bodyString);
        } catch (JsonException) {
            return WebhookHttpResult.BadRequest("Invalid OmniMessaging webhook JSON.");
        }

        using (document) {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) {
                return WebhookHttpResult.BadRequest("OmniMessaging webhook payload must be a JSON object.");
            }

            if (TryParseDeliveryReport(root, out var deliveryEvent, out var deliveryError)) {
                return WebhookHttpResult.Ok([deliveryEvent], statusCode: 200, successPayload: new JsonObject {
                    ["success"] = true
                });
            }

            if (TryParseInteraction(root, out var interactionEvent, out var interactionError)) {
                return WebhookHttpResult.Ok([interactionEvent], statusCode: 200, successPayload: new JsonObject {
                    ["success"] = true
                });
            }

            return WebhookHttpResult.BadRequest(deliveryError, interactionError, "Unsupported OmniMessaging webhook payload format.");
        }
    }

    private static bool TryParseDeliveryReport(JsonElement root, out WebhookEvent webhookEvent, out string error) {
        webhookEvent = null!;
        error = string.Empty;

        if (!root.TryGetProperty("delivery_report", out var deliveryReport) || deliveryReport.ValueKind != JsonValueKind.Object) {
            error = "Missing 'delivery_report' object.";
            return false;
        }

        var recipient = GetJsonValueAsString(root, "to");
        if (string.IsNullOrWhiteSpace(recipient)) {
            error = "Delivery report webhook requires 'to'.";
            return false;
        }

        var rootId = GetJsonValueAsString(root, "id");
        var messageId = GetJsonValueAsString(root, "message_id");
        var campaignId = GetJsonValueAsString(root, "campaign_id");
        var resolvedMessageId = messageId ?? campaignId ?? rootId;
        if (string.IsNullOrWhiteSpace(resolvedMessageId)) {
            error = "Delivery report webhook requires one of 'message_id', 'campaign_id' or 'id'.";
            return false;
        }
        var status = GetJsonValueAsString(deliveryReport, "status");
        var dateRaw = GetJsonValueAsString(deliveryReport, "date");

        var timestamp = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(dateRaw) && !DateTimeOffset.TryParse(dateRaw, out timestamp)) {
            error = "Invalid 'delivery_report.date' value.";
            return false;
        }

        webhookEvent = new WebhookEvent {
            Provider = "OmniMessaging",
            EventType = string.IsNullOrWhiteSpace(status) ? "SmsDelivery" : status,
            MessageId = resolvedMessageId,
            EventId = rootId ?? resolvedMessageId,
            Recipient = recipient,
            Subject = GetJsonValueAsString(root, "campaign_reference"),
            Timestamp = timestamp,
            Payload = JsonNode.Parse(root.GetRawText()),
            Succeeded = EventSucceeded(string.IsNullOrWhiteSpace(status) ? "SmsDelivery" : status),
            Channel = "SMS"
        };

        return true;
    }


    private static bool TryParseInteraction(JsonElement root, out WebhookEvent webhookEvent, out string error) {
        webhookEvent = null!;
        error = string.Empty;

        if (!root.TryGetProperty("webhook_data", out var webhookData) || webhookData.ValueKind != JsonValueKind.Object) {
            error = "Missing 'webhook_data' object.";
            return false;
        }

        var recipient = GetJsonValueAsString(webhookData, "to");
        if (string.IsNullOrWhiteSpace(recipient)) {
            error = "Interaction webhook requires 'webhook_data.to'.";
            return false;
        }

        var rootId = GetJsonValueAsString(root, "id");
        var messageId = GetJsonValueAsString(webhookData, "message_id");
        var interaction = GetJsonValueAsString(webhookData, "interaction");
        var webhookType = GetJsonValueAsString(root, "webhook_type");
        var campaignId = GetJsonValueAsString(webhookData, "campaign_id");
        var resolvedMessageId = messageId ?? campaignId ?? rootId;
        if (string.IsNullOrWhiteSpace(resolvedMessageId)) {
            error = "Interaction webhook requires one of 'webhook_data.message_id', 'webhook_data.campaign_id' or 'id'.";
            return false;
        }

        var timestamp = DateTimeOffset.UtcNow;
        if (webhookData.TryGetProperty("interaction_data", out var interactionData) &&
            interactionData.ValueKind == JsonValueKind.Object) {
            var dateRaw = GetJsonValueAsString(interactionData, "date");
            if (!string.IsNullOrWhiteSpace(dateRaw) && !DateTimeOffset.TryParse(dateRaw, out timestamp)) {
                error = "Invalid 'webhook_data.interaction_data.date' value.";
                return false;
            }
        }

        webhookEvent = new WebhookEvent {
            Provider = "OmniMessaging",
            EventType = interaction ?? webhookType ?? "Interaction",
            MessageId = resolvedMessageId,
            EventId = rootId ?? resolvedMessageId,
            Recipient = recipient,
            Subject = GetJsonValueAsString(webhookData, "campaign_reference"),
            Timestamp = timestamp,
            Payload = JsonNode.Parse(root.GetRawText()),
            Succeeded = EventSucceeded(interaction ?? webhookType ?? "Interaction"),
            Channel = "SMS"
        };

        return true;
    }

    private static bool EventSucceeded(string eventType) {
        return eventType.Equals("delivered", StringComparison.OrdinalIgnoreCase) || eventType.Equals("sent", StringComparison.OrdinalIgnoreCase);
    }
    private static string? GetJsonValueAsString(JsonElement json, string propertyName) {
        if (!json.TryGetProperty(propertyName, out var property)) {
            return null;
        }

        return property.ValueKind switch {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Null => null,
            _ => property.GetRawText()
        };
    }

    private bool IsAuthorized(IDictionary<string, StringValues> requestHeaders)
        => _settings.WebhookAuthenticationMethod switch {
            SmsServiceOmniMessagingWebhookAuthenticationMethod.ApiKey => IsApiKeyAuthorized(requestHeaders),
            SmsServiceOmniMessagingWebhookAuthenticationMethod.Basic => IsBasicAuthorized(requestHeaders),
            _ => false
        };

    private bool IsApiKeyAuthorized(IDictionary<string, StringValues> requestHeaders) {
        if (string.IsNullOrWhiteSpace(_settings.WebhookSecret)) {
            return false;
        }

        var headers = requestHeaders.ToDictionary(h => h.Key, h => h.Value, StringComparer.OrdinalIgnoreCase);
        var headerName = string.IsNullOrWhiteSpace(_settings.WebhookApiKeyHeaderName) ? "X-API-Key" : _settings.WebhookApiKeyHeaderName;
        var provided = headers.GetValueOrDefault(headerName).ToString();
        return IsEqual(provided, _settings.WebhookSecret);
    }

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

    private static bool IsEqual(string? left, string? right) {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) {
            return false;
        }

        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        if (leftBytes.Length != rightBytes.Length) {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}

