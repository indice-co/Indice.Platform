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
/// Viber/SMS service implementation using the Apifon IM service gateway.
/// https://docs.apifon.com/apireference.html#im-gateway-rest-api
/// </summary>
public class SmsServiceApifonIM : ISmsService
{
    /// <summary>The Apifon base URL address.</summary>
    internal static readonly string APIFON_BASE_URL = "https://ars.apifon.com";
    /// <summary>The Apifon IM service gateway endpoint.</summary>
    internal static readonly string SERVICE_ENDPOINT = "/services/api/v1/im/send";
    /// <summary>The settings required to configure the service.</summary>
    protected SmsServiceApifonSettings Options { get; }
    /// <summary>The <see cref="System.Net.Http.HttpClient"/>.</summary>
    protected HttpClient HttpClient { get; }
    /// <summary>Represents a type used to perform logging.</summary>
    protected ILogger<SmsServiceApifonIM> Logger { get; }

    /// <inheritdoc/>
    public SmsServiceApifonIM(
        HttpClient httpClient,
        IOptionsSnapshot<SmsServiceApifonSettings> options,
        ILogger<SmsServiceApifonIM> logger) {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (string.IsNullOrWhiteSpace(Options.Token)) {
            throw new ArgumentException($"SMS settings {nameof(SmsServiceApifonSettings.Token)} is empty.");
        }
        if (string.IsNullOrWhiteSpace(Options.ApiKey)) {
            throw new ArgumentException($"SMS settings {nameof(SmsServiceApifonSettings.ApiKey)} is empty.");
        }
    }

    /// <inheritdoc/>
    public async Task<SendReceipt> SendAsync(string destination, string subject, string body, SmsSender sender = null) {
        HttpResponseMessage httpResponse;
        ApifonResponse response;
        var recipients = (destination ?? string.Empty).Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
        if (recipients == null) {
            throw new ArgumentNullException(nameof(recipients));
        }
        if (recipients.Length == 0) {
            throw new ArgumentException("Recipients list cannot be empty.", nameof(recipients));
        }
        recipients = recipients.Select(recipient => {
            if (!PhoneNumber.TryParse(recipient, out var phone)) {
                throw new ArgumentException("Invalid recipients. Recipients should be valid phone numbers", nameof(recipients));
            }
            return phone.ToString("D");
        }).ToArray();
        if (recipients.Any(phoneNumber => phoneNumber.Any(numberChar => !char.IsNumber(numberChar)))) {
            throw new ArgumentException("Invalid recipients. Recipients cannot contain letters.", nameof(recipients));
        }
        var senderId = sender?.Id ?? Options.Sender ?? Options.SenderName;
        var payload = new ApifonIMRequest(senderId, recipients, body) {
            IMChannels = [new() { 
                SenderId = senderId,
                Text = body
            }]
        };
        var signature = payload.Sign(Options.ApiKey, HttpMethod.Post.ToString(), SERVICE_ENDPOINT);
        var request = new HttpRequestMessage {
            Content = new StringContent(payload.ToJson(), Encoding.UTF8, MediaTypeNames.Application.Json),
            Method = HttpMethod.Post,
            RequestUri = HttpClient.BaseAddress
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        request.Headers.Add("X-ApifonWS-Date", payload.RequestDate.ToString("r"));
        request.Headers.Authorization = new AuthenticationHeaderValue("ApifonWS", $"{Options.Token}:{signature}");
        try {
            Logger.LogInformation("The full request sent to Apifon: {requestMessage}", JsonSerializer.Serialize(request, GetJsonSerializerOptions()));
            Logger.LogInformation("The following payload was sent to Apifon: {requestPayload}", payload.ToJson());
            httpResponse = await HttpClient.SendAsync(request);
        } catch (Exception ex) {
            Logger.LogError("Viber/SMS Delivery took too long");
            throw new SmsServiceException("Viber/SMS Delivery took too long", ex);
        }
        var responseString = await httpResponse.Content.ReadAsStringAsync();
        if (!httpResponse.IsSuccessStatusCode) {
            Logger.LogInformation("Viber/SMS Delivery failed. {statusCode} : {responseString}", httpResponse.StatusCode, responseString);
            throw new SmsServiceException($"Viber/SMS Delivery failed. {httpResponse.StatusCode} : {responseString}");
        }
        response = JsonSerializer.Deserialize<ApifonResponse>(responseString, GetJsonSerializerOptions());
        if (response.HasError) {
            Logger.LogInformation("Viber/SMS Delivery failed. {responseStatus}. ResponseId: {responseId}", response.Status.Description, response.Id);
            throw new SmsServiceException($"Viber/SMS Delivery failed. {response.Status.Description} responseId {response.Id}");
        } else {
            Logger.LogInformation("Viber/SMS message successfully sent: {result}", response.Results.FirstOrDefault());
        }
        var messageIds = response.Results?.Values
          .SelectMany(x => x?.Select(y => y.Id))?
          .Where(id => !string.IsNullOrWhiteSpace(id))
          .ToList();
        var messageId = messageIds?.Count > 0
            ? string.Join(",", messageIds)
            : response.Id;
        return new SendReceipt(messageId, DateTimeOffset.UtcNow);
    }

    /// <summary>Checks the implementation if supports the given <paramref name="deliveryChannel"/>.</summary>
    /// <param name="deliveryChannel">A string representing the delivery channel. i.e 'SMS'</param>
    /// <returns></returns>
    public bool Supports(string deliveryChannel) => 
        "SMS".Equals(deliveryChannel, StringComparison.OrdinalIgnoreCase) || 
        "Viber".Equals(deliveryChannel, StringComparison.OrdinalIgnoreCase);

    /// <summary>Get default JSON serializer options: CamelCase, ignore null values.</summary>
    protected static JsonSerializerOptions GetJsonSerializerOptions() => new JsonSerializerOptions {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

internal class ApifonIMRequest : ApifonRequest {
    public ApifonIMRequest(string from, string[] to, string message) : base(from, to, message) { }

    [JsonPropertyName("im_channels")]
    public List<IMChannel> IMChannels { get; set; } = new List<IMChannel>();

    /// <inheritdoc/>
    public override string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    });
}

internal class IMChannel {
    [JsonPropertyName("sender_id")]
    public string SenderId { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("ttl")]
    public int SecondsUntilSmsFailover { get; set; } = 30;
}