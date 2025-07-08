using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Indice.Extensions;
using Indice.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Services;

/// <summary>SMS service implementation using the SMSUP SMS service gateway.</summary>
public class SmsServiceSmsUp : ISmsService
{
    /// <summary>The SMSUP base URL address.</summary>
    internal static readonly string SMSUP_BASE_URL = "https://api.gateway360.com/api/3.0/sms/send";
    /// <summary>The <see cref="System.Net.Http.HttpClient"/>.</summary>
    protected HttpClient HttpClient { get; }
    /// <summary>Represents a type used to perform logging.</summary>
    protected ILogger<SmsServiceSmsUp> Logger { get; }
    /// <summary>The settings required to configure the service.</summary>
    protected SmsServiceSmsUpSettings Settings { get; }

    /// <inheritdoc/>
    public SmsServiceSmsUp(
        HttpClient httpClient,
        IOptionsSnapshot<SmsServiceSmsUpSettings> settings,
        ILogger<SmsServiceSmsUp> logger) {
        Settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        HttpClient.DefaultRequestHeaders.Accept.Clear();
        HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <inheritdoc/>
    public async Task<SendReceipt> SendAsync(string destination, string subject, string? body, SmsSender? sender = null) {
        if (string.IsNullOrWhiteSpace(destination))
            throw new ArgumentNullException(nameof(destination), "Recipient is empty.");

        if (!PhoneNumber.TryParse(destination, out var phone))
            throw new ArgumentException("Invalid recipient phone number.", nameof(destination));

        var request = new SmsUpRequest {
            ApiKey = Settings.ApiKey!,
            ReportUrl = Settings.ReportUrl,
            Concat = Settings.Concat,
            Fake = Settings.Fake,
            Messages = new List<SmsUpMessage>
            {
                new SmsUpMessage
                {
                    From = sender?.Id ?? Settings.Sender!, // fallback sender
                    To = phone.ToString("D"),
                    Text = body ?? subject ?? string.Empty,
                    Custom = Guid.NewGuid().ToString()
                }
            }
        };

        var requestJson = JsonSerializer.Serialize(request, GetJsonSerializerOptions());
        var content = new StringContent(requestJson, Encoding.UTF8, MediaTypeNames.Application.Json);

        HttpResponseMessage httpResponse;
        try {
            Logger.LogInformation("Sending SMS via smsUp to {To}", phone.ToString("D"));
            httpResponse = await HttpClient.PostAsync(SMSUP_BASE_URL, content);
        } catch (Exception ex) {
            Logger.LogError(ex, "Error sending SMS");
            throw new SmsServiceException("Error occurred while sending SMS", ex);
        }

        var responseContent = await httpResponse.Content.ReadAsStringAsync();
        if (!httpResponse.IsSuccessStatusCode) {
            Logger.LogWarning("SMS send failed: {StatusCode} - {Response}", httpResponse.StatusCode, responseContent);
            throw new SmsServiceException($"SmsUp SMS failed: {httpResponse.StatusCode} - {responseContent}");
        }

        var response = JsonSerializer.Deserialize<SmsApiResponse>(responseContent, GetJsonSerializerOptions())!;
        var result = response.Result;

        if (result == null || result.FirstOrDefault()?.Status != "ok") {
            Logger.LogWarning("SmsUp error: {ErrorId} - {ErrorMsg}", result?.FirstOrDefault()?.ErrorId ?? response.ErrorId, result?.FirstOrDefault()?.ErrorMsg ?? response.ErrorMsg);
            throw new SmsServiceException($"SmsUp error: {result?.FirstOrDefault()?.ErrorMsg ?? response.ErrorMsg}");
        }else {
            var messageIds = string.Join(",", result.Select(r => r.SmsId));
            return new SendReceipt(messageIds, DateTimeOffset.UtcNow);
        }          
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

/// <summary>Extra settings class for configuring SMSUP SMS service client. </summary>
public class SmsServiceSmsUpSettings
{
    /// <summary>Key in the configuration.</summary>
    public static readonly string Name = "Sms";

    /// <summary>The API key.</summary>
    public string? ApiKey { get; set; }

    /// <summary>The Report URL for delivery reports.</summary>
    public string? ReportUrl { get; set; }

    /// <summary>Whether to enable message concatenation (1 = true, 0 = false).</summary>
    public int Concat { get; set; } = 1;

    /// <summary>Sender.</summary>
    public string? Sender {  get; set; }

    /// <summary>With value 0 the message is sent. For testing purposes Fake must be 1.</summary>
    public int Fake { get; set; } = 0;

    /// <summary>Gets the Authorization header or credentials format (if applicable).</summary>
    public string GetAuthorizationHeader() {
        if (string.IsNullOrWhiteSpace(ApiKey)) {
            throw new ArgumentException("API key must be provided.");
        }

        // In SmsUp, API key is typically passed in the body, but you can format it like a bearer if needed
        return $"ApiKey {ApiKey}";
    }
}


internal class SmsUpRequest
{
    [JsonPropertyName("api_key")]
    public string ApiKey { get; set; } = default!;

    [JsonPropertyName("report_url")]
    public string? ReportUrl { get; set; }

    [JsonPropertyName("concat")]
    public int Concat { get; set; }

    [JsonPropertyName("fake")]
    public int Fake { get; set; }

    [JsonPropertyName("messages")]
    public List<SmsUpMessage> Messages { get; set; } = new();
}

internal class SmsUpMessage
{
    [JsonPropertyName("from")]
    public string From { get; set; } = default!;

    [JsonPropertyName("to")]
    public string To { get; set; } = default!;

    [JsonPropertyName("text")]
    public string Text { get; set; } = default!;

    [JsonPropertyName("custom")]
    public string? Custom { get; set; }

    [JsonPropertyName("send_at")]
    public string? SendAt { get; set; }
}


internal class SmsApiResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("result")]
    public List<SmsApiResultItem>? Result { get; set; }

    [JsonPropertyName("error_id")]
    public string? ErrorId { get; set; }

    [JsonPropertyName("error_msg")]
    public string? ErrorMsg { get; set; }
}

internal class SmsApiResultItem
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("sms_id")]
    public string? SmsId { get; set; }

    [JsonPropertyName("custom")]
    public string? Custom { get; set; }

    [JsonPropertyName("error_id")]
    public string? ErrorId { get; set; }

    [JsonPropertyName("error_msg")]
    public string? ErrorMsg { get; set; }
}
