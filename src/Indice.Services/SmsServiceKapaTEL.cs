using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
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
        var param = Settings.GetType()
                            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            .ToDictionary(prop => prop.Name.ToLower(), prop => (string?)prop.GetValue(Settings, null));
        param.Add("text", body);
        param.Add("to", string.Join(',', recipients));
        param.Add("format", "json");
        if (!string.IsNullOrWhiteSpace(Settings.WebhookUrl)) {
            param.Add("dlr_url", Settings.WebhookUrl);
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
        if (response.Status.Equals("error")) {
            Logger.LogError("SMS Delivery failed. {responseError}", response.ErrorMessage);
            throw new SmsServiceException($"SMS Delivery failed. {response.ErrorMessage}");
        }
        Logger.LogInformation("SMS message successfully sent: {messages}", string.Join(",", response.Messages.Select(x => $"{x.Msisdn}-{x.ErrorMessage}")));
        if (response.Messages?.Count > 0) {
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
        var messageId =
            parameters.GetValueOrDefault("id").ToString() ??
            parameters.GetValueOrDefault("remote_transaction_id").ToString() ??
            parameters.GetValueOrDefault("message_id").ToString();
        if (string.IsNullOrWhiteSpace(messageId)) {
            return WebhookHttpResult.BadRequest("KapaTEL webhook requires one of 'id', 'remote_transaction_id' or 'message_id' query parameters.");
        }

        var foreignId = parameters.GetValueOrDefault("foreign_id").ToString();
        var recipient =
            parameters.GetValueOrDefault("msisdn").ToString() ??
            parameters.GetValueOrDefault("receiver").ToString() ??
            parameters.GetValueOrDefault("to").ToString() ??
            parameters.GetValueOrDefault("phone").ToString();
        if (string.IsNullOrWhiteSpace(recipient)) {
            recipient = foreignId;
        }

        var sender =
            parameters.GetValueOrDefault("from").ToString() ??
            parameters.GetValueOrDefault("sender").ToString();

        var notificationStatus =
            parameters.GetValueOrDefault("notification_status").ToString() ??
            parameters.GetValueOrDefault("status").ToString();

        var notificationTimestamp =
            parameters.GetValueOrDefault("notification_datetime_stamp").ToString() ??
            parameters.GetValueOrDefault("dlrDate").ToString();

        var timestamp = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(notificationTimestamp) && !DateTimeOffset.TryParse(notificationTimestamp, out timestamp)) {
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
                Recipient = recipient ?? string.Empty,
                Subject = sender,
                Timestamp = timestamp,
                Payload = payload
            }
        };

        await Task.CompletedTask;
        return WebhookHttpResult.Ok(incomingEvents);
    }

    private bool IsBasicAuthenticationInvalid(IDictionary<string, StringValues> requestHeaders) {
        var hasConfiguredWebhookCredentials = !string.IsNullOrWhiteSpace(_settings.WebhookUsername) || !string.IsNullOrWhiteSpace(_settings.WebhookPassword);
        if (!hasConfiguredWebhookCredentials) {
            return true;
        }
        if (string.IsNullOrWhiteSpace(_settings.WebhookUsername) || string.IsNullOrWhiteSpace(_settings.WebhookPassword)) {
            return true;
        }

        if (!TryGetHeaderValue(requestHeaders, "Authorization", out var authorizationHeader) ||
            !AuthenticationHeaderValue.TryParse(authorizationHeader, out var authorization) ||
            !"Basic".Equals(authorization.Scheme, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(authorization.Parameter)) {
            return true;
        }

        string decodedCredentials;
        try {
            decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(authorization.Parameter));
        } catch (FormatException) {
            return true;
        } catch (ArgumentException) {
            return true;
        }

        var separatorIndex = decodedCredentials.IndexOf(':');
        if (separatorIndex <= 0 || separatorIndex == decodedCredentials.Length - 1) {
            return true;
        }

        var username = decodedCredentials[..separatorIndex];
        var password = decodedCredentials[(separatorIndex + 1)..];

        return !AreEqualConstantTime(username, _settings.WebhookUsername) ||
               !AreEqualConstantTime(password, _settings.WebhookPassword);
    }

    private static bool TryGetHeaderValue(IDictionary<string, StringValues> headers, string key, out string value) {
        value = string.Empty;
        foreach (var header in headers) {
            if (string.Equals(header.Key, key, StringComparison.OrdinalIgnoreCase)) {
                value = header.Value.ToString();
                return !string.IsNullOrWhiteSpace(value);
            }
        }
        return false;
    }

    private static bool AreEqualConstantTime(string left, string? right) {
        if (right is null) {
            return false;
        }

        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
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