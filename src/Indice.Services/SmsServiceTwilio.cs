using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Indice.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Services;

/// <summary>SMS service implementation using the TWILIO SMS service gateway.</summary>
public class SmsServiceTwilio : ISmsService
{
    /// <summary>The TWILIO base URL address.</summary>
    internal static readonly string TWILIO_BASE_URL = "https://api.twilio.com/2010-04-01";
    /// <summary>The <see cref="System.Net.Http.HttpClient"/>.</summary>
    protected HttpClient HttpClient { get; }
    /// <summary>Represents a type used to perform logging.</summary>
    protected ILogger<SmsServiceTwilio> Logger { get; }
    /// <summary>The settings required to configure the service.</summary>
    protected SmsServiceTwilioSettings Settings { get; }

    /// <inheritdoc/>
    public SmsServiceTwilio(
        HttpClient httpClient,
        IOptionsSnapshot<SmsServiceTwilioSettings> settings,
        ILogger<SmsServiceTwilio> logger) {
        Settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(Settings.AccountSid)) {
            throw new ArgumentException("AccountSid must not be empty.", nameof(Settings.AccountSid));
        }
        if (string.IsNullOrWhiteSpace(Settings.Secret)) {
            throw new ArgumentException("ApiKeySecret must not be empty.", nameof(Settings.Secret));
        }

        string credentials = !string.IsNullOrWhiteSpace(Settings.ApiKey)
                            ? $"{Settings.ApiKey}:{Settings.Secret}"
                            : $"{Settings.AccountSid}:{Settings.Secret}";

        string authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));

        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        HttpClient.DefaultRequestHeaders.Add("Accept", MediaTypeNames.Application.Json);
    }

    /// <inheritdoc/>
    public async Task<SendReceipt> SendAsync(string destination, string subject, string? body, SmsSender? sender = null) {
        if (string.IsNullOrWhiteSpace(destination)) {
            throw new ArgumentNullException(nameof(destination), "Recipient is empty.");
        }
        if (destination.Contains(',')) {
            throw new NotSupportedException("Only a single recipient phone number is supported.");
        }
        if (!PhoneNumber.TryParse(destination, out var phone)) {
            throw new ArgumentException("Invalid recipient phone number.", nameof(destination));
        }

        var requestUri = $"{TWILIO_BASE_URL}/Accounts/{Settings.AccountSid}/Messages.json";

        var formFields = new Dictionary<string, string> {
            ["To"] = phone.ToString("D"),
            ["Body"] = body ?? string.Empty
        };

        // Use MessagingServiceSid if present; otherwise use From number
        if (!string.IsNullOrWhiteSpace(Settings.MessagingServiceSid)) {
            formFields["MessagingServiceSid"] = Settings.MessagingServiceSid!;
        } else {
            var from = sender?.Id ?? Settings.SenderPhoneNumber;
            if (string.IsNullOrWhiteSpace(from)) {
                throw new ArgumentException("SenderPhoneNumber or MessagingServiceSid must be provided.");
            }
            formFields["From"] = from;
        }

        using var content = new FormUrlEncodedContent(formFields);

        HttpResponseMessage httpResponse;
        try {
            Logger.LogInformation("Sending SMS via Twilio to {To}", phone.ToString("D"));
            httpResponse = await HttpClient.PostAsync(requestUri, content);
        } catch (HttpRequestException ex) {
            Logger.LogError(ex, "HTTP request error occurred during SMS delivery");
            throw new SmsServiceException("HTTP request error occurred during SMS delivery", ex);
        } catch (TaskCanceledException ex) {
            Logger.LogError(ex, "SMS delivery request timed out");
            throw new SmsServiceException("SMS delivery request timed out", ex);
        } catch (Exception ex) {
            Logger.LogError(ex, "Unexpected error during SMS delivery");
            throw new SmsServiceException("Unexpected error during SMS delivery", ex);
        }

        var responseContent = await httpResponse.Content.ReadAsStringAsync();
        if (!httpResponse.IsSuccessStatusCode) {
            Logger.LogInformation("SMS delivery failed: {StatusCode} - {Response}", httpResponse.StatusCode, responseContent);
            throw new SmsServiceException($"Twilio SMS delivery failed: {httpResponse.StatusCode} - {responseContent}");
        }

        var response = JsonSerializer.Deserialize<TwilioSmsResponse>(responseContent, GetJsonSerializerOptions())!;
        if (!string.IsNullOrWhiteSpace(response.ErrorCode)) {
            Logger.LogInformation("Twilio responded with error: {Error}", response.ErrorMessage);
            throw new SmsServiceException($"Twilio error: {response.ErrorMessage}");
        }

        Logger.LogInformation("SMS sent successfully with SID: {Sid}", response.Sid);
        return new SendReceipt(response.Sid!, DateTimeOffset.UtcNow);
    }

    /// <summary>Checks the implementation if supports the given <paramref name="deliveryChannel"/>.</summary>
    /// <param name="deliveryChannel">A string representing the delivery channel. i.e 'SMS'</param>
    /// <returns></returns>
    public bool Supports(string deliveryChannel) => "SMS".Equals(deliveryChannel, StringComparison.OrdinalIgnoreCase);

    /// <summary>Get default JSON serializer options.</summary>
    protected static JsonSerializerOptions GetJsonSerializerOptions() => new JsonSerializerOptions {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

/// <summary>Extra settings class for configuring TWILIO SMS service client. </summary>
public class SmsServiceTwilioSettings : SmsServiceSettings
{
    /// <summary>The Secret.</summary>
    public string? Secret { get; set; }
    /// <summary>The Account Sid.</summary>
    public string? AccountSid { get; set; }
    /// <summary>The Sender Phone Number.</summary>
    public string? SenderPhoneNumber { get; set; }
    /// <summary>The Messaging Service Sid.</summary>
    public string? MessagingServiceSid { get; set; }
}

internal class TwilioSmsResponse
{
    [JsonPropertyName("sid")]
    public string? Sid { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("to")]
    public string? To { get; set; }

    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("date_created")]
    public string? DateCreated { get; set; }

    [JsonPropertyName("date_sent")]
    public string? DateSent { get; set; }

    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    [JsonPropertyName("subresource_uris")]
    public Dictionary<string, string>? SubresourceUris { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("code")]
    public int? Code { get; set; }
}
