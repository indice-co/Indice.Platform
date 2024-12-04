using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Indice.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    public async Task<SendReceipt> SendAsync(string destination, string subject, string body, SmsSender sender = null) {
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
                            .ToDictionary(prop => prop.Name.ToLower(), prop => (string)prop.GetValue(Settings, null));
        param.Add("text", body);
        param.Add("to", string.Join(',', recipients));
        param.Add("format", "json");
        var requestUri = new Uri(QueryHelpers.AddQueryString(HttpClient.BaseAddress.ToString(), param));
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
        response = JsonSerializer.Deserialize<KapaTELResponse>(responseString, GetJsonSerializerOptions());
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
    public string Username { get; set; }
    /// <summary>Password In unicode URL-encoded format.</summary>
    public string Password { get; set; }
    /// <summary>Choose Between GSM7 encoding and UTF8.</summary>
    public string Encoding { get; set; } = "utf8";
    /// <summary>Alphanumeric Sender ID from where to send the message (max 11 characters.Allowed characters are Latin alphabet and characters “dot”, “space”, “hyphen”).</summary>
    public string From { get; set; }
}

internal class KapaTELResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; }

    [JsonPropertyName("error_message")]
    public string ErrorMessage { get; set; }

    internal class Message
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("foreign_id")]
        public string ForeignId { get; set; }

        [JsonPropertyName("msisdn")]
        public string Msisdn { get; set; }

        [JsonPropertyName("error_message")]
        public string ErrorMessage { get; set; }
    }
}