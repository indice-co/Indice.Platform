using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Services;

/// <summary>SMS service implementation using the Apifon SMS service gateway.</summary>
public class SmsServiceApifon : ISmsService
{
    /// <summary>Constructs the <see cref="SmsServiceApifon"/> using the <seealso cref="SmsServiceSettings"/>.</summary>
    /// <param name="settings">The settings required to configure the service.</param>
    /// <param name="httpClient">Injected <see cref="System.Net.Http.HttpClient"/> managed by the DI.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public SmsServiceApifon(
        HttpClient httpClient, 
        IOptionsSnapshot<SmsServiceApifonSettings> settings, 
        ILogger<SmsServiceApifon> logger
    ) {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (string.IsNullOrWhiteSpace(Settings.Token)) {
            throw new ArgumentException($"SMS settings {nameof(SmsServiceApifonSettings.Token)} is empty.");
        }
        if (string.IsNullOrWhiteSpace(Settings.ApiKey)) {
            throw new ArgumentException($"SMS settings {nameof(SmsServiceApifonSettings.ApiKey)} is empty.");
        }
    }

    /// <summary>The settings required to configure the service.</summary>
    protected SmsServiceApifonSettings Settings { get; }
    /// <summary>The <see cref="System.Net.Http.HttpClient"/>.</summary>
    protected HttpClient HttpClient { get; }
    /// <summary>Represents a type used to perform logging.</summary>
    protected ILogger<SmsServiceApifon> Logger { get; }

    /// <inheritdoc/>
    public async Task SendAsync(string destination, string subject, string body, SmsSender sender = null) {
        HttpResponseMessage httpResponse;
        ApifonResponse response;
        var recipients = (destination ?? string.Empty).Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
        if (recipients == null) {
            throw new ArgumentNullException(nameof(recipients));
        }
        if (recipients.Length == 0) {
            throw new ArgumentException("Recipients list cannot be empty.", nameof(recipients));
        }
        if (recipients.Any(phoneNumber => phoneNumber.Any(numberChar => !char.IsNumber(numberChar)))) {
            throw new ArgumentException("Invalid recipients. Recipients cannot contain letters.", nameof(recipients));
        }
        // TODO: Create a universal way to handle country codes.
        // Quick and ugly fix for Apifon which only accepts MSISDN and no leading zeroes -> https://docs.apifon.com/apireference.html#sms-request
        recipients = recipients.Select(
            phoneNumber => phoneNumber.StartsWith("30")
                ? phoneNumber
                : phoneNumber.StartsWith("0") ? phoneNumber.TrimStart('0') : $"30{phoneNumber}"
        )
        .ToArray();
        var payload = new ApifonRequest(sender?.Id ?? Settings.Sender ?? Settings.SenderName, recipients, body);
        var signature = payload.Sign(Settings.ApiKey, HttpMethod.Post.ToString(), "/services/api/v1/sms/send");
        var request = new HttpRequestMessage {
            Content = new StringContent(payload.ToJson(), Encoding.UTF8, "application/json"),
            Method = HttpMethod.Post,
            RequestUri = new Uri(HttpClient.BaseAddress, "send")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-ApifonWS-Date", payload.RequestDate.ToString("r"));
        request.Headers.Authorization = new AuthenticationHeaderValue("ApifonWS", $"{Settings.Token}:{signature}");
        try {
            Logger.LogInformation("The full request sent to Apifon: {0}", JsonSerializer.Serialize(request, GetJsonSerializerOptions()));
            Logger.LogInformation("The following payload was sent to Apifon: {0}", payload.ToJson());
            httpResponse = await HttpClient.SendAsync(request);
        } catch (Exception ex) {
            Logger.LogInformation("SMS Delivery took too long: {0}", ex);
            throw new SmsServiceException($"SMS Delivery took too long", ex);
        }
        var responseString = await httpResponse.Content.ReadAsStringAsync();
        if (!httpResponse.IsSuccessStatusCode) {
            Logger.LogInformation($"SMS Delivery failed. {httpResponse.StatusCode} : {responseString}");
            throw new SmsServiceException($"SMS Delivery failed. {httpResponse.StatusCode} : {responseString}");
        }
        response = JsonSerializer.Deserialize<ApifonResponse>(responseString, GetJsonSerializerOptions());
        if (response.HasError) {
            Logger.LogInformation($"SMS Delivery failed. {response.Status.Description}");
            throw new SmsServiceException($"SMS Delivery failed. {response.Status.Description}");
        } else {
            Logger.LogInformation("SMS message successfully sent: {1}", response.Results.FirstOrDefault());
        }
    }

    /// <summary>Checks the implementation if supports the given <paramref name="deliveryChannel"/>.</summary>
    /// <param name="deliveryChannel">A string representing the delivery channel. i.e 'SMS'</param>
    /// <returns></returns>
    public bool Supports(string deliveryChannel) => "SMS".Equals(deliveryChannel, StringComparison.OrdinalIgnoreCase);

    /// <summary>Get default JSON serializer options: CamelCase, ignore null values.</summary>
    protected static JsonSerializerOptions GetJsonSerializerOptions() => new JsonSerializerOptions {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

/// <summary>Options for configuring <see cref="SmsServiceApifon"/>.</summary>
public class SmsServiceApifonOptions
{
    /// <summary>Optional options for <see cref="HttpMessageHandler"/></summary>
    public Func<IServiceProvider, HttpMessageHandler> ConfigurePrimaryHttpMessageHandler { get; set; }
}

/// <summary>Extra settings class for configuring Apifon SMS service client. </summary>
public class SmsServiceApifonSettings : SmsServiceSettings
{
    /// <summary>Apifon Api token key.</summary>
    public string Token { get; set; }
}

internal class ApifonResponse
{
    [JsonPropertyName("request_id")]
    public string Id { get; set; }
    public Dictionary<string, ResultDetails[]> Results { get; set; }
    [JsonPropertyName("result_info")]
    public ResultInfo Status { get; set; }
    public bool HasError => !(Status?.StatusCode >= 200 && Status?.StatusCode < 300);

    internal class ResultDetails
    {
        [JsonPropertyName("message_id")]
        public string Id { get; set; }
        [JsonPropertyName("custom_id")]
        public string CustomId { get; set; }
        public int Length { get; set; }
        [JsonPropertyName("short_url")]
        public string ShortUrl { get; set; }
        [JsonPropertyName("short_code")]
        public string ShortCode { get; set; }
    }

    internal class ResultInfo
    {
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }
        public string Description { get; set; }
    }
}

internal class ApifonRequest
{
    public ApifonRequest(string from, string[] to, string message) {
        foreach (var subNumber in to) {
            Subscribers.Add(new Subscribers { To = subNumber });
        }
        Message.From = from;
        Message.Text = message;
    }

    [JsonPropertyName("message")]
    public Message Message { get; set; } = new Message();
    [JsonPropertyName("subscribers")]
    public List<Subscribers> Subscribers { get; set; } = new List<Subscribers>();
    /// <summary>SMS validity period. Min 30 - Max 4320 (default).</summary>
    [JsonPropertyName("tte")]
    public int? ValidityPeriod { get; set; }
    /// <summary>If set, the callback (delivery / status report) will be delivered to this URL, otherwise no callback will take place.</summary>
    [JsonPropertyName("callback_url")]
    public string CallbackUrl { get; set; }
    /// <summary>The date that the message will be sent on UTC/GMT TIMEZONE. If omitted it will be sent immediately.</summary>
    [JsonPropertyName("date")]
    public DateTime? DateToSend { get; set; }
    [JsonIgnore]
    public DateTime RequestDate { get; set; } = DateTime.Now.ToUniversalTime();

    /// <summary>Serialize our concrete class into a JSON String.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    });

    public string Sign(string secretKey, string method, string uri) {
        var toSign = method + "\n"
            + uri + "\n"
            + ToJson() + "\n"
            + RequestDate.ToString("r");
        var encoding = new UTF8Encoding();
        using (var hmacSha256 = new HMACSHA256(encoding.GetBytes(secretKey))) {
            return Convert.ToBase64String(hmacSha256.ComputeHash(encoding.GetBytes(toSign)));
        }
    }
}

internal class Message
{
    /// <summary>
    /// Contains the body of the SMS message to be delivered to the destination device. One can optionally specify keys within the message body that will be replaced later with values given by 
    /// the parameters field in the SUBSCRIBERS* object. See 'parameters' in the SUBSCRIBERS* section for more information on supplying the value for these keys. Each placeholder must be specified as 
    /// { KEY}, where KEY is a key name in the parameters list. For this feature to be used, GSM7 or UCS2 encoding must be used. In the event your text is longer than 160 characters in 7bit, 140 in 
    /// 8bit or 70 in 16bit, Apifon will split the message into parts.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; }
    /// <summary>
    /// Contains the information on how the text is encoded.
    /// The maximum number of characters you can fit into a single message depends on the encoding you are using:
    /// 
    /// [0]: GSM7 bit Default Alphabet(160).
    /// Basic Latin subset of ASCII, as well as some characters of the ISO Latin 1 – 160
    /// 
    /// [1]: GSM 8-bit data encoding(140.
    /// 8-bit data encoding mode treats the information as raw data.According to the standard, the alphabet for this encoding is user-specific.
    /// 
    /// [2]: UCS-2 (UTF-16 [16 bit]) Encoding(70).
    /// This encoding allows use of a greater range of characters and languages.UCS-2 can represent the most commonly used Latin and eastern
    /// characters at the cost of a greater space expense.
    /// </summary>
    [JsonPropertyName("dc")]
    public string Encoding { get; set; }
    /// <summary>Numeric (maximum number of digits: 16) or alphanumeric characters (maximum number of characters: 11).</summary>
    [JsonPropertyName("sender_id")]
    public string From { get; set; }
}

internal class Subscribers
{
    /// <summary>Mobile number to deliver the message to. Number is in international format and is only digits between 7-15 digits long. First digit cannot be a 0.</summary>
    [JsonPropertyName("number")]
    public string To { get; set; }
    /// <summary>If your message content contains placeholders for personalized messages per destination, this field is required to populate the value for each recipient.</summary>
    public Dictionary<string, string> Params { get; set; }
}
