using System.Globalization;
using System.Net;
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

/// <summary>SMS service implementation using the VONAGE SMS service gateway.</summary>
public class SmsServiceVonage : ISmsService
{
    /// <summary>The VONAGE base URL address.</summary>
    internal static readonly string VONAGE_BASE_URL = "https://rest.nexmo.com";
    /// <summary>The VONAGE SMS API service gateway endpoint.</summary>
    internal static readonly string SERVICE_ENDPOINT = "/sms/json";
    /// <summary>The <see cref="System.Net.Http.HttpClient"/>.</summary>
    protected HttpClient HttpClient { get; }
    /// <summary>Represents a type used to perform logging.</summary>
    protected ILogger<SmsServiceVonage> Logger { get; }
    /// <summary>The settings required to configure the service.</summary>
    protected SmsServiceVonageSettings Settings { get; }

    /// <inheritdoc/>
    public SmsServiceVonage(
        HttpClient httpClient,
        IOptionsSnapshot<SmsServiceVonageSettings> settings,
        ILogger<SmsServiceVonage> logger) {
        Settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (string.IsNullOrWhiteSpace(Settings.ApiKey)) {
            throw new ArgumentException($"SMS settings {nameof(SmsServiceVonageSettings.ApiKey)} is empty.");
        }
        if (string.IsNullOrWhiteSpace(Settings.SignatureSecret)) {
            throw new ArgumentException($"SMS settings {nameof(SmsServiceVonageSettings.SignatureSecret)} is empty.");
        }
        HttpClient.DefaultRequestHeaders.Add("Accept", MediaTypeNames.Application.Json);
    }

    /// <inheritdoc/>
    public async Task<SendReceipt> SendAsync(string destination, string subject, string? body, SmsSender? sender = null) {
        if (string.IsNullOrWhiteSpace(destination)) {
            throw new ArgumentNullException("Recipient is empty" ,nameof(destination));
        }
        if (destination.IndexOf(',') >= 0) {
            throw new NotSupportedException("Only a single recipient phone number is supported.");
        }
        if (!PhoneNumber.TryParse(destination, out var phone)) {
            throw new ArgumentException("Invalid recipient. Recipient should be valid phone numbers", nameof(destination));
        }
        var request = new VonageSmsRequest(
            apiKey: Settings.ApiKey!,
            from: sender?.Id ?? Settings.Sender ?? Settings.SenderName!,
            to: phone.ToString("D"),
            text: body!,
            ttl: Settings.Ttl
        )
        .ToHttpRequest(new Uri($"{VONAGE_BASE_URL}{SERVICE_ENDPOINT}"), Settings.SignatureSecret!);
        HttpResponseMessage httpResponse;

        try {
            Logger.LogInformation("The following payload was sent to VONAGE: {RequestPayload}", JsonSerializer.Serialize(request, GetJsonSerializerOptions()));
            httpResponse = await HttpClient.SendAsync(request);
        } catch (HttpRequestException ex) {
            Logger.LogError(ex, "HTTP request error occurred during SMS delivery");
            throw new SmsServiceException("HTTP request error occurred during SMS delivery", ex);
        } catch (TaskCanceledException ex) {
            Logger.LogError(ex, "SMS delivery request timed out");
            throw new SmsServiceException("SMS delivery request timed out", ex);
        } catch (Exception ex) {
            Logger.LogError(ex, "An unexpected error occurred during SMS delivery");
            throw new SmsServiceException("An unexpected error occurred during SMS delivery", ex);
        }

        var responseString = await httpResponse.Content.ReadAsStringAsync();
        if (!httpResponse.IsSuccessStatusCode) {
            Logger.LogInformation("SMS Delivery failed. {StatusCode} : {ResponseString}", httpResponse.StatusCode, responseString);
            throw new SmsServiceException($"SMS Delivery failed. {httpResponse.StatusCode} : {responseString}");
        }

        var response = JsonSerializer.Deserialize<VonageSmsResponse>(responseString, GetJsonSerializerOptions())!;
        if (response.HasError) {
            Logger.LogInformation("SMS Delivery failed with reason: {Reason}", response.ErrorText);
            throw new SmsServiceException($"SMS Delivery failed with reason: {response.ErrorText}");
        } else {
            Logger.LogInformation("SMS message successfully sent");
        }

        var messageIds = string.Join(",", response.Messages.Select(m => m.MessageId));
        return new SendReceipt(messageIds, DateTimeOffset.UtcNow);
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

/// <summary>Extra settings class for configuring VONAGE SMS service client. </summary>
public class SmsServiceVonageSettings : SmsServiceSettings
{
    /// <summary>The signature secret.</summary>
    public string? SignatureSecret { get; set; }
    /// <summary>The duration in milliseconds the delivery of an SMS will be attempted.</summary>
    public int Ttl { get; set; } = 20000;
}

internal class VonageSmsRequest
{
    private readonly SortedDictionary<string, string> _requestParams;

    public VonageSmsRequest(string apiKey, string from, string to, string text, int ttl) {
        _requestParams = new SortedDictionary<string, string>
        {
            ["api_key"] = apiKey,
            ["to"] = to,
            ["from"] = from,
            ["text"] = text,
            ["ttl"] = ttl.ToString(CultureInfo.InvariantCulture),
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)
        };
    }

    public HttpRequestMessage ToHttpRequest(Uri uri, string signatureSecret) {
        var queryString = BuildQueryString(signatureSecret);
        var content = new StringContent(queryString, new MediaTypeHeaderValue(MediaTypeNames.Application.FormUrlEncoded));
        return new HttpRequestMessage(HttpMethod.Post, uri) { Content = content };
    }

    private string BuildQueryString(string signatureSecret) {
        var queryToSign = string.Join("&", _requestParams.Select(kvp =>
            $"{kvp.Key.Replace('=', '_').Replace('&', '_')}={kvp.Value.Replace('=', '_').Replace('&', '_')}"));

        var signature = GenerateSha256Signature($"&{queryToSign}", signatureSecret);

        var encodedQuery = string.Join("&", _requestParams.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={(kvp.Key == "ids" ? kvp.Value : Uri.EscapeDataString(kvp.Value))}"));

        return $"{encodedQuery}&sig={signature}";
    }

    private static string GenerateSha256Signature(string data, string key) {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "");
    }
}

internal class VonageSmsResponse {
    [JsonPropertyName("message-count")]
    public string? MessageCount { get; set; }
    [JsonPropertyName("messages")]
    public List<VonageSmsResponseMessage> Messages { get; set; } = [];

    public bool HasError => Messages.Any(x => x.Status != "0");

    public string? ErrorText => Messages.FirstOrDefault()?.ErrorText ?? string.Empty;
}

internal class VonageSmsResponseMessage {
    [JsonPropertyName("to")]
    public string? To { get; set; }
    [JsonPropertyName("message-id")]
    public string? MessageId { get; set; }
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    [JsonPropertyName("network")]
    public string? Network { get; set; }
    [JsonPropertyName("client-ref")]
    public string? ClientRef { get; set; }
    [JsonPropertyName("error-text")]
    public string? ErrorText { get; set; }
}