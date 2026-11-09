using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Globalization;
using Indice.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Indice.Services;

/// <summary>SMS service implementation using the KapaTEL SMS service gateway.</summary>
public class SmsServiceKapaTEL : ISmsService
{
    /// <summary>Constructs the <see cref="SmsServiceKapaTEL"/> using the <seealso cref="SmsServiceSettings"/>.</summary>
    /// <param name="settings">The settings required to configure the service.</param>
    /// <param name="httpClient">Injected <see cref="System.Net.Http.HttpClient"/> managed by the DI.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public SmsServiceKapaTEL(
        HttpClient httpClient, 
        IOptionsSnapshot<SmsServiceKapaTELSettings> settings, 
        ILogger<SmsServiceKapaTEL> logger
    ) {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>The <see cref="System.Net.Http.HttpClient"/>.</summary>
    protected HttpClient HttpClient { get; }
    /// <summary>The settings required to configure the service.</summary>
    protected SmsServiceKapaTELSettings Settings { get; }
    /// <summary>Represents a type used to perform logging.</summary>
    protected ILogger<SmsServiceKapaTEL> Logger { get; }

    /// <inheritdoc/>
    public async Task<SendReceipt> SendAsync(string destination, string subject, string? body, SmsSender? sender = null) {
        var messageId = Guid.NewGuid().ToString();
        HttpResponseMessage httpResponse;
        KapaTELResponse response;
        var recipients = (destination ?? string.Empty).Split(new string[] { ",", ";" }, StringSplitOptions.RemoveEmptyEntries);
        if (recipients == null) {
            throw new ArgumentNullException(nameof(recipients));
        }
        if (recipients.Length == 0) {
            throw new ArgumentException("Recipients list cannot be empty.", nameof(recipients));
        }
        if (recipients.Length > 10) {
            throw new ArgumentException("Recipients list can contain up to 10 MSISDN.", nameof(recipients));
        }
        recipients = recipients.Select(recipient => {
            if (!PhoneNumber.TryParse(recipient, out var phone)) {
                throw new ArgumentException("Invalid recipients. Recipients should be valid phone numbers", nameof(recipients));
            }
            return phone.ToString("N");
        })
        .ToArray();
        if (recipients.Any(phoneNumber => phoneNumber.Any(numberChar => !char.IsNumber(numberChar)))) {
            throw new ArgumentException("Invalid recipients. Recipients cannot contain letters.", nameof(recipients));
        }
        var param = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) {
            ["username"] = Settings.Username,
            ["password"] = Settings.Password,
            ["encoding"] = Settings.Encoding,
            ["from"] = Settings.From,
            ["text"] = body,
            ["to"] = string.Join(',', recipients),
            ["format"] = "json",
            ["foreign_id"] = messageId
        };
        if (!string.IsNullOrWhiteSpace(Settings.WebhookUrl)) {
            param["dlr_url"] = BuildDlrUrl(Settings.WebhookUrl);
        }
        var requestUri = new Uri(QueryHelpers.AddQueryString(HttpClient.BaseAddress!.ToString(), param));
        try {
            Logger.LogInformation("The request Uri sent to KapaTEL: {requestUri}", requestUri);
            httpResponse = await HttpClient.PostAsync(requestUri, null);
        } catch (Exception ex) {
            Logger.LogError("SMS Delivery took too long");
            throw new SmsServiceException($"SMS Delivery took too long", ex);
        }
        var responseString = await httpResponse.Content.ReadAsStringAsync();
        if (!httpResponse.IsSuccessStatusCode) {
            Logger.LogError("SMS Delivery failed. {statusCode} : {responseString}", httpResponse.StatusCode, responseString);
            throw new SmsServiceException($"SMS Delivery failed. {httpResponse.StatusCode} : {responseString}");
        }
        response = JsonSerializer.Deserialize<KapaTELResponse>(responseString, GetJsonSerializerOptions())!;
        if (response.Status.Equals("error", StringComparison.OrdinalIgnoreCase)) {
            Logger.LogError("SMS Delivery failed. {responseError}", response.ErrorMessage);
            throw new SmsServiceException($"SMS Delivery failed. {response.ErrorMessage}");
        }
        if (!response.Status.Equals("accepted", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(response.ErrorMessage)) {
            Logger.LogError("SMS Delivery failed. {responseError}", response.ErrorMessage);
            throw new SmsServiceException($"SMS Delivery failed. {response.ErrorMessage}");
        }
        Logger.LogInformation("SMS message successfully sent. Status: {status}, MessageId: {messageId}", response.Status, response.MessageId);
        if (!string.IsNullOrWhiteSpace(response.MessageId)) {
            messageId = response.MessageId;
        } else if (response.Messages?.Count > 0) {
            messageId = string.Join(",", response.Messages.Select(x => x.Id));
        }
        return new SendReceipt(messageId, DateTimeOffset.UtcNow);
    }

    /// <summary>Checks the implementation if supports the given <paramref name="deliveryChannel"/>.</summary>
    /// <param name="deliveryChannel">A string representing the delivery channel. i.e 'SMS'.</param>
    public bool Supports(string deliveryChannel) => "SMS".Equals(deliveryChannel, StringComparison.OrdinalIgnoreCase);

    /// <summary>Get default JSON serializer options: CamelCase, ignore null values.</summary>
    protected static JsonSerializerOptions GetJsonSerializerOptions() => new() {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static string BuildDlrUrl(string webhookUrl) {
        const string MessageIdQuery = "message_id={foreign_id}";
        const string RemoteTransactionIdQuery = "remote_transaction_id={id}";
        const string StatusQuery = "status={notification_status}";
        const string NotificationDateTimeQuery = "notification_datetime={notification_datetime_stamp}";

        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri)) {
            return webhookUrl;
        }

        var existingParameters = QueryHelpers.ParseQuery(uri.Query);
        var missingParameters = new List<string>();
        if (!existingParameters.ContainsKey("message_id")) {
            missingParameters.Add(MessageIdQuery);
        }
        if (!existingParameters.ContainsKey("remote_transaction_id")) {
            missingParameters.Add(RemoteTransactionIdQuery);
        }
        if (!existingParameters.ContainsKey("status")) {
            missingParameters.Add(StatusQuery);
        }
        if (!existingParameters.ContainsKey("notification_datetime")) {
            missingParameters.Add(NotificationDateTimeQuery);
        }

        if (missingParameters.Count == 0) {
            return webhookUrl;
        }

        var urlWithoutFragment = webhookUrl;
        var fragment = string.Empty;
        var fragmentIndex = webhookUrl.IndexOf('#');
        if (fragmentIndex >= 0) {
            urlWithoutFragment = webhookUrl[..fragmentIndex];
            fragment = webhookUrl[fragmentIndex..];
        }

        var separator = urlWithoutFragment.Contains('?') ? '&' : '?';
        return $"{urlWithoutFragment}{separator}{string.Join("&", missingParameters)}{fragment}";
    }
}

/// <summary>Extra settings class for configuring KapaTEL SMS service client.</summary>
public class SmsServiceKapaTELSettings
{
    /// <summary>Username of your account in unicode URL-encoded format.</summary>
    public string? Username { get; set; }
    /// <summary>Password In unicode URL-encoded format.</summary>
    public string? Password { get; set; }
    /// <summary>Choose Between GSM7 encoding and UTF8.</summary>
    public string Encoding { get; set; } = "utf8";
    /// <summary>Alphanumeric Sender ID from where to send the message (max 11 characters.Allowed characters are Latin alphabet and characters “dot”, “space”, “hyphen”).</summary>
    public string? From { get; set; }
    /// <summary>The URL to receive delivery notifications (DLR).</summary>
    public string? WebhookUrl { get; set; }
    /// <summary>Optional webhook basic auth username for validating incoming DLR callbacks.</summary>
    public string? WebhookUsername { get; set; }
    /// <summary>Optional webhook basic auth password for validating incoming DLR callbacks.</summary>
    public string? WebhookPassword { get; set; }
}

internal class KapaTELResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; } = [];

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    internal class Message
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("foreign_id")]
        public string? ForeignId { get; set; }

        [JsonPropertyName("msisdn")]
        public string? Msisdn { get; set; }

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }
    }
}

/// <inheritdoc/>
public sealed class KapaTELWebhookHttpHandler : IWebhookHttpHandler {
    private readonly SmsServiceKapaTELSettings _settings;

    /// <inheritdoc/>
    public KapaTELWebhookHttpHandler(IOptionsSnapshot<SmsServiceKapaTELSettings> settings) {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc/>
    public async Task<WebhookHttpResult> ParseAsync(
        IDictionary<string, StringValues> requestHeaders,
        IEnumerable<KeyValuePair<string, StringValues>> requestParameters,
        Stream requestBody,
        CancellationToken cancellationToken) {
        if (IsBasicAuthenticationInvalid(requestHeaders)) {
            return WebhookHttpResult.UnauthorizedRequest("Invalid KapaTEL webhook basic authentication.");
        }

        var parameters = requestParameters.ToDictionary(parameter => parameter.Key, parameter => parameter.Value, StringComparer.OrdinalIgnoreCase);
        var messageId = GetFirstNonEmptyParameter(parameters, "remote_transaction_id", "id");
        if (string.IsNullOrWhiteSpace(messageId)) {
            return WebhookHttpResult.BadRequest("KapaTEL webhook requires one of 'id', 'remote_transaction_id' or 'message_id' query parameters.");
        }

        var notificationStatus = GetFirstNonEmptyParameter(parameters, "notification_status", "status");

        var notificationTimestamp = GetFirstNonEmptyParameter(parameters, "notification_datetime", "notification_datetime_stamp", "dlrDate");

        var timestamp = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(notificationTimestamp) && !TryParseNotificationTimestamp(notificationTimestamp, out timestamp)) {
            return WebhookHttpResult.BadRequest("Invalid notification timestamp in KapaTEL webhook payload.");
        }

        var payload = new JsonObject();
        foreach (var parameter in parameters) {
            payload[parameter.Key] = parameter.Value.ToString();
        }

        var eventType = GetEventType(notificationStatus);
        var eventId = !string.IsNullOrWhiteSpace(notificationStatus) ? $"{messageId}:{notificationStatus}:{timestamp:O}" : $"{messageId}:{timestamp:O}";
        var incomingEvents = new List<WebhookEvent> {
            new() {
                Provider = "KapaTEL",
                EventType = eventType,
                EventId = eventId,
                MessageId = messageId,
                Timestamp = timestamp,
                Payload = payload
            }
        };

        await Task.CompletedTask;
        return WebhookHttpResult.Ok(incomingEvents, statusCode: 204);
    }

    private static bool TryParseNotificationTimestamp(string value, out DateTimeOffset timestamp) {
        return DateTimeOffset.TryParseExact(
                   value,
                   ["yyMMddHHmmss", "yyyyMMddHHmmss"],
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.AssumeLocal,
                   out timestamp) ||
               DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out timestamp) ||
               DateTimeOffset.TryParse(value, out timestamp);
    }

    private static string GetFirstNonEmptyParameter(IDictionary<string, StringValues> parameters, params string[] keys) {
        foreach (var key in keys) {
            if (parameters.TryGetValue(key, out var value)) {
                var candidate = value.ToString();
                if (!string.IsNullOrWhiteSpace(candidate)) {
                    return candidate;
                }
            }
        }
        return string.Empty;
    }

    private bool IsBasicAuthenticationInvalid(IDictionary<string, StringValues> requestHeaders) {
        if (string.IsNullOrWhiteSpace(_settings.WebhookUsername) && string.IsNullOrWhiteSpace(_settings.WebhookPassword)) {
            return false;
        }

        if (!requestHeaders.TryGetValue("Authorization", out var authorizationValues)) {
            return true;
        }

        var authorization = authorizationValues.ToString();
        if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        return !WebhookAuthentication.IsValidBasicAuthentication(authorization, _settings.WebhookUsername!, _settings.WebhookPassword!);
    }

    private static string GetEventType(string? notificationStatus) {
        return notificationStatus switch {
            "1" => "DeliveredToPhone",
            "2" => "NonDeliveredToPhone",
            "8" => "DeliveredToSMSC",
            "16" => "NonDeliveredToSMSC",
            _ when !string.IsNullOrWhiteSpace(notificationStatus) => $"Status:{notificationStatus}",
            _ => "SmsDelivery"
        };
    }
}