using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Indice.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Services;

/// <summary>Service to send SMS via Konecta API</summary>
public class SmsServiceKonecta : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly SmsServiceKonectaSettings _settings;
    private readonly ILogger<SmsServiceKonecta> _logger;

    /// <summary>Creates a new instance of <see cref="SmsServiceKonecta"/>.</summary>
    /// <param name="httpClient">Provides a base class for sending HTTP requests and receiving HTTP responses from a resource identified by a URI.</param>
    /// <param name="settings">Settings class for configuring SMS service clients.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public SmsServiceKonecta(
        HttpClient httpClient,
        IOptionsSnapshot<SmsServiceKonectaSettings> settings,
        ILogger<SmsServiceKonecta> logger
    ) {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (_httpClient.BaseAddress == null) {
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl ?? "https://service.comdatagroup.fr/rcs/api/v1/");
        }

        if (!_httpClient.DefaultRequestHeaders.Contains("Authorization")) {
            if (string.IsNullOrWhiteSpace(_settings.Username) || string.IsNullOrWhiteSpace(_settings.Password)) {
                throw new InvalidOperationException("Username and Password are required for Konecta SMS service.");
            }
            var credentials = $"{_settings.Username}:{_settings.Password}";
            var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);
        }

        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <inheritdoc />
    public async Task<SendReceipt> SendAsync(string destination, string subject, string? body, SmsSender? sender = null) {
        var phoneNumbers = GetRecipientsFromDestination(destination);

        if (phoneNumbers.Length > 1) {
            throw new SmsServiceException("Konecta SMS service does not support multiple recipients in a single request.");
        }

        var recipient = phoneNumbers[0];
        var messageId = Guid.NewGuid().ToString();

        var requestBody = new SendRequest {
            Sender = sender?.Id ?? _settings.Sender ?? _settings.SenderName ?? throw new InvalidOperationException("Sender is required."),
            Recipient = recipient,
            Content = CreateContent(body),
            Operation = _settings.Operation ?? "campaign",
            Site = _settings.Site ?? "default"
        };

        var jsonData = JsonSerializer.Serialize(requestBody, GetJsonSerializerOptions());
        _logger.LogDebug("The following payload was sent to Konecta: {requestBody}", jsonData);

        using var data = new StringContent(jsonData, Encoding.UTF8, "application/json");
        using var httpResponseMessage = await _httpClient.PostAsync("rcs/api/v1/message/send", data);

        if (!httpResponseMessage.IsSuccessStatusCode) {
            var errorMessage = "SMS Delivery failed.\n";
            if (httpResponseMessage.Content != null) {
                errorMessage += await httpResponseMessage.Content.ReadAsStringAsync();
            }
            _logger.LogError(errorMessage);
            throw new SmsServiceException(errorMessage);
        }

        var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();
        _logger.LogDebug("The following response was received from Konecta: {responseContent}", responseContent);

        var response = JsonSerializer.Deserialize<SendResponse>(responseContent, GetJsonSerializerOptions());

        if (response?.Success != true) {
            _logger.LogError("SMS Delivery failed: {responseMessage}", response?.Message ?? "Unknown error");
            throw new SmsServiceException($"SMS Delivery failed: {response?.Message ?? "Unknown error"}");
        }

        _logger.LogInformation("SMS message successfully sent.");

        // Extract message ID from the data field if available
        if (!string.IsNullOrEmpty(response.Data)) {
            try {
                var dataObj = JsonSerializer.Deserialize<JsonElement>(response.Data);
                if (dataObj.TryGetProperty("infobip", out var infobip) &&
                    infobip.TryGetProperty("messages", out var messages) &&
                    messages.GetArrayLength() > 0) {
                    var firstMessage = messages[0];
                    if (firstMessage.TryGetProperty("messageId", out var msgId)) {
                        messageId = msgId.GetString() ?? messageId;
                    }
                }
            } catch (JsonException ex) {
                _logger.LogWarning(ex, "Could not parse message ID from response data");
            } catch (InvalidOperationException ex) {
                _logger.LogWarning(ex, "Could not parse message ID from response data");
            }
        }

        return new SendReceipt(messageId, DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public bool Supports(string deliveryChannel) => "SMS".Equals(deliveryChannel, StringComparison.OrdinalIgnoreCase);

    #region Helper Methods
    /// <summary>Get list of phone numbers from destination.</summary>
    /// <param name="destination"></param>
    private string[] GetRecipientsFromDestination(string destination) {
        var recipients = (destination ?? string.Empty).Split([","], StringSplitOptions.RemoveEmptyEntries);
        if (recipients == null) {
            throw new ArgumentNullException(nameof(recipients));
        }
        if (recipients.Length == 0) {
            throw new ArgumentException("Recipients list is empty.", nameof(recipients));
        }

        recipients = recipients.Select(recipient => {
            if (!PhoneNumber.TryParse(recipient, out var phone)) {
                throw new ArgumentException("Invalid recipients. Recipients should be valid phone numbers", nameof(recipients));
            }
            return phone.ToString("D");
        })
        .ToArray();

        if (recipients.Any(telephone => telephone.Any(character => !char.IsNumber(character)))) {
            throw new ArgumentException("Invalid recipients. Recipients cannot contain letters.", nameof(recipients));
        }

        return recipients;
    }

    /// <summary>Get default Json Serializer Options: CamelCase, ignore null values.</summary>
    private JsonSerializerOptions GetJsonSerializerOptions() => new JsonSerializerOptions {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static string CreateContent(string? body) {
        var value = body ?? string.Empty;

        value = value
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");

        return $"{{'type':'TEXT','text':'{value}'}}";
    }

    #endregion

    #region Models
    internal class SendRequest
    {
        /// <summary>The sender identifier</summary>
        [JsonPropertyName("sender")]
        public string? Sender { get; set; }

        /// <summary>The recipient phone number</summary>
        [JsonPropertyName("recipient")]
        public string? Recipient { get; set; }

        /// <summary>The message content as JSON string</summary>
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        /// <summary>The operation identifier</summary>
        [JsonPropertyName("operation")]
        public string? Operation { get; set; }

        /// <summary>The site identifier</summary>
        [JsonPropertyName("site")]
        public string? Site { get; set; }
    }

    internal class SendResponse
    {
        /// <summary>Indicates if the request was successful</summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>The response message</summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>The response data as JSON string</summary>
        [JsonPropertyName("data")]
        public string? Data { get; set; }
    }
    #endregion
}

/// <summary>Settings class for configuring Konecta SMS service.</summary>
public class SmsServiceKonectaSettings : SmsServiceSettings
{
    /// <summary>The base URL for the Konecta API. Defaults to https://service.comdatagroup.fr/rcs/api/v1/</summary>
    public string? BaseUrl { get; set; }

    /// <summary>The username for Basic authentication</summary>
    public string? Username { get; set; }

    /// <summary>The password for Basic authentication</summary>
    public string? Password { get; set; }

    /// <summary>The operation identifier for SMS campaigns</summary>
    public string? Operation { get; set; }

    /// <summary>The site identifier</summary>
    public string? Site { get; set; }
}
