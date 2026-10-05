using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Indice.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

        if (Options.ViberValidity < 30) {
            throw new ArgumentException($"SMS settings {nameof(SmsServiceOmniMessagingSettings.ViberValidity)} must be at least 30 seconds.");
        }

        if (Options.ViberValidity > 1209600) {
            throw new ArgumentException($"SMS settings {nameof(SmsServiceOmniMessagingSettings.ViberValidity)} cannot exceed 1209600 seconds.");
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
            Options.ViberFallbackEnabled,
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
        "Viber".Equals(deliveryChannel, StringComparison.OrdinalIgnoreCase) ||
        ("SMS".Equals(deliveryChannel, StringComparison.OrdinalIgnoreCase) && Options.ViberFallbackEnabled);

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
    /// Gets or sets a value indicating whether SMS fallback is enabled when Viber delivery fails.
    /// </summary>
    public bool ViberFallbackEnabled { get; set; }

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
        bool viberFallbackEnabled,
        int viberValidity,
        int smsValidity,
        bool useUtf8) {
            return new SmsServiceOmniRequest {
                Name = sender,
                Recipients = recipients
                    .Select(phoneNumber => new OmniRecipient {
                        UniqueId = phoneNumber,
                        Mobile = phoneNumber
                    })
                    .ToList(),
                Content = new OmniContent {
                    Sms = new OmniSms {
                        From = sender,
                        Text = message,
                        Charset = (useUtf8) ? "UTF-8" : "GSM"
                    },
                    Viber = viberFallbackEnabled
                        ? new OmniViber {
                            Message = new OmniViberMessage {
                                Text = message,
                            }
                        }
                        : null
                },
                Scheduling = new OmniScheduling {
                    Channels = new OmniSchedulingChannels {
                        Sms = new OmniChannelScheduling {
                            TimePeriod = smsValidity
                        },
                        Viber = viberFallbackEnabled
                            ? new OmniChannelScheduling {
                                TimePeriod = viberValidity
                            }
                            : null
                    },
                    Fallback = viberFallbackEnabled
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
    public required OmniSms Sms { get; set; }

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
    public string Charset { get; init; } = "GSM";
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
    public string? RequestId { get; set; }

    public JsonElement? Data { get; set; }

    public string? GetCampaignId() {
        if (!string.IsNullOrWhiteSpace(Id)) {
            return Id;
        }

        if (!string.IsNullOrWhiteSpace(CampaignId)) {
            return CampaignId;
        }

        if (!string.IsNullOrWhiteSpace(RequestId)) {
            return RequestId;
        }

        if (Data is { ValueKind: JsonValueKind.Object } data) {
            if (data.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String) {
                return id.GetString();
            }

            if (data.TryGetProperty("campaign_id", out var campaignId) && campaignId.ValueKind == JsonValueKind.String) {
                return campaignId.GetString();
            }

            if (data.TryGetProperty("request_id", out var requestId) && requestId.ValueKind == JsonValueKind.String) {
                return requestId.GetString();
            }
        }

        return null;
    }
}
