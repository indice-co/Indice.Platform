using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Indice.Extensions;
using Indice.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Services;

/// <remarks></remarks>
public class SmsServiceMstat : ISmsService
{
    /// <summary>Constructs the <see cref="SmsServiceMstat"/> using the <seealso cref="SmsServiceSettings"/>.</summary>
    /// <param name="settings">The settings required to configure the service.</param>
    /// <param name="httpClient">Injected <see cref="HttpClient"/> managed by the DI.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public SmsServiceMstat(
        HttpClient httpClient,
        IOptionsSnapshot<SmsServiceMstatSettings> settings,
        ILogger<SmsServiceMstat> logger
    ) {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (string.IsNullOrWhiteSpace(Settings.ApiKey)) {
            throw new ArgumentException($"SMS settings {nameof(SmsServiceMstatSettings.ApiKey)} is empty.");
        }
    }
    /// <summary>The settings required to configure the service.</summary>
    protected SmsServiceMstatSettings Settings { get; }
    /// <summary>The <see cref="System.Net.Http.HttpClient"/>.</summary>
    protected HttpClient HttpClient { get; }
    /// <summary>Represents a type used to perform logging.</summary>
    protected ILogger<SmsServiceMstat> Logger { get; }

    /// <inheritdoc/>
    public async Task<SendReceipt> SendAsync(string destination, string subject, string? body, SmsSender? sender = null) {
        HttpResponseMessage httpResponse;
        MstatResponse response;

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
        var uniqueId = Settings.GenerateUniqueId ? Guid.NewGuid().ToString() : null;
        var payload = recipients.Select(phoneNumber => new MstatRequest(ViberMessageType.OneWayTextSmartphones, message: body, Settings.SenderName!, Settings.Channel, uniqueId, phoneNumber)).ToList();
        var payloadJson = JsonSerializer.Serialize(payload, GetJsonSerializerOptions());
        var request = new HttpRequestMessage {
            Content = new StringContent(payloadJson, Encoding.UTF8, MediaTypeNames.Application.Json),
            Method = HttpMethod.Post,
            RequestUri = new Uri(HttpClient.BaseAddress!.ToString())
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        request.Headers.Add("API-Token", Settings.ApiKey);

        try {
            Logger.LogDebug("The full request sent to Mstat: {Request}", JsonSerializer.Serialize(request, GetJsonSerializerOptions()));
            httpResponse = await HttpClient.SendAsync(request);
        } catch (Exception ex) {
            Logger.LogError(ex, "Message Delivery took too long");
            throw new SmsServiceException("Message Delivery took too long", ex);
        }

        var responseString = await httpResponse.Content.ReadAsStringAsync();
        if (!httpResponse.IsSuccessStatusCode) {
            Logger.LogInformation("Message Delivery failed. Status: {StatusCode} : {ResponseJson}", httpResponse.StatusCode, responseString);
            throw new SmsServiceException($"Message Delivery failed. {httpResponse.StatusCode} : {responseString}");
        }
        response = JsonSerializer.Deserialize<MstatResponse>(responseString, GetJsonSerializerOptions())!;

        var responseDataList = response.Data.ToObject<List<MstatResponse.ResponseData>>(GetJsonSerializerOptions());
        var errorData = responseDataList.Where(x => x.HasErrros).ToList();
        if (errorData.Count > 0) {
            foreach (var data in errorData) {
                Logger.LogInformation("Message Delivery failed for number: {Destination} - Error: {ErrorMessage}", destination, data.Errors.FirstOrDefault());
            }
        }

        Logger.LogInformation("Message to Number: {Destination} - UniqueId: {UniqueId}", destination, uniqueId ?? responseDataList.Where(x => !x.HasErrros)
                                                                                                                                  .Select(x => x.UniqueId)
                                                                                                                                  .FirstOrDefault());
        if (responseDataList?.Where(x => !x.HasErrros).Count() > 0) {
            uniqueId = string.Join(',', responseDataList.Select(x => x.UniqueId));
        }
        return new SendReceipt(uniqueId!, DateTimeOffset.UtcNow);
    }

    /// <summary>Checks the implementation if supports the given <paramref name="deliveryChannel"/>.</summary>
    /// <param name="deliveryChannel">A string representing the delivery channel. i.e 'SMS'.</param>
    public bool Supports(string deliveryChannel) => "SMS".Equals(deliveryChannel, StringComparison.OrdinalIgnoreCase);

    /// <summary>Get default JSON serializer options: CamelCase, ignore null values.</summary>
    protected static JsonSerializerOptions GetJsonSerializerOptions() => new JsonSerializerOptions {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal class MstatRequest
    {

        /// <summary>Sender ID that a client is allowed to use when sending Viber ServiceMessages</summary>
        [JsonPropertyName("channel")]
        public string? Channel { get; set; }

        [JsonPropertyName("unique_id")]
        public string UniqueId { get; set; }

        /// <summary>Phone Number</summary>
        [JsonPropertyName("destination")]
        public string? Destination { get; set; }

        /// <summary>
        /// A JSON array that contains the message payload for the various types of messages. 
        /// The order that the objects appear in this element definethe order that the delivery attempts will be made.
        /// <br/>
        /// For example when a Viber object is followed by an SMS object the platform attempts to send the Viber message first.
        /// </summary>
        [JsonPropertyName("flow")]
        public List<MstatFlow>? Flows { get; set; }

        public MstatRequest(ViberMessageType type, string? message, string from, string? channel, string? uniqueId, string destination) {
            Channel = channel;
            UniqueId = string.IsNullOrEmpty(uniqueId) ? "auto" : uniqueId;
            Destination = destination;
            Flows = [
                MstatFlow.ForViber(type, message),
                MstatFlow.ForSms(from, message)
            ];
        }
    }


    internal class MstatFlow
    {

        [JsonPropertyName("viber")]
        public MstatViber? Viber { get; set; }

        [JsonPropertyName("sms")]
        public MstatSms? Sms { get; set; }

        public MstatFlow() {

        }

        public static MstatFlow ForSms(string from, string? message) => new MstatFlow {
            Sms = new MstatSms(from, message)
        };

        public static MstatFlow ForViber(ViberMessageType type, string? message) => new MstatFlow {
            Viber = new MstatViber(type, message)
        };

        internal class MstatViber
        {
            /// <summary> A JSON object that defines the observable parameters of the Viber message. </summary>
            [JsonPropertyName("message")]
            public MstatViberMessage? ViberMessage { get; set; }

            /// <summary> The message type of the Viber message. </summary>
            /// <value>You can find a description of Viber message types in the Documentation </value>
            [JsonPropertyName("type")]
            public int Type { get; set; }

            /// <summary>The time in seconds after which the message expires.</summary>
            /// <value>
            /// The values are between 60 and 86400 seconds (24 hours).
            /// <br/>
            /// The default validity period if this setting is not supplied is 2 weeks.
            /// </value>
            [JsonPropertyName("validity_period")]
            public int ValidityPeriod { get; set; } = 60;

            public MstatViber() {

            }

            public MstatViber(ViberMessageType type, string? message) {
                Type = (int)type;
                ViberMessage = new MstatViberMessage(message);
            }
        }

        internal class MstatSms
        {
            /// <summary>A JSON object that defines the observable parameters of the SMS message.</summary>
            [JsonPropertyName("message")]
            public MstatSmsMessage? SmsMessage { get; set; }

            /// <summary>
            /// The charset of the SMS message that will be delivered to the recipient. 
            /// This setting affects the number of characters allowed per SMS part as well as the allowed characters.
            /// </summary>
            /// <value>
            /// Possible values are GSM and UTF-8 and the default value is GSM
            /// </value>
            [JsonPropertyName("charset")]
            public string? Charset { get; set; } = "GSM";

            /// <summary>
            /// The number of seconds that the SMS is considered valid. 
            /// At the end of the validity period the SMS will be considered as Expired and no further attempts to deliver the message will be made.
            /// </summary>
            /// <value>
            /// The minimum value of the validity_period field for SMS messages is 120 seconds and the maximum is 172800 (48 hours).
            /// <br/>
            /// In case this field is not provided or the value is out of range the message is sent having the maximum validity period allowed (48 hours).
            /// </value>
            [JsonPropertyName("validity_period")]
            public int ValidityPeriod { get; set; } = 120;

            public MstatSms() {

            }

            public MstatSms(string from, string? message) {
                SmsMessage = new MstatSmsMessage(from, message);
            }
        }

    }

    internal class MstatViberMessage
    {
        /// <summary> The textual part of the Viber message. Can be up to 1000 Unicode characters. </summary>
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        /// <summary>
        /// A Viber message can contain a call to action button. 
        /// This parameters defines the button caption that can be up to 30 Unicode characters.
        /// </summary>
        [JsonPropertyName("caption")]
        public string? Caption { get; set; }

        /// <summary>
        /// There are various options to define the action that will happen when the user clicks on the action button.
        /// </summary>
        /// <value>
        /// See the documentation for more: M-STAT_TMS_Fallback_1.5 - Section 6.1 Message Format
        /// </value>
        [JsonPropertyName("action")]
        public string? Action { get; set; }

        /// <summary>
        /// A Viber message can contain an optional image that is displayed inside the message. 
        /// <br/>
        /// The image will be fit in a square placeholder by the Viber mobile application so it is suggested to use square images.
        /// </summary>
        [JsonPropertyName("image")]
        public string? Image { get; set; }

        public MstatViberMessage() {

        }

        public MstatViberMessage(string? text) {
            Text = text;
        }

    }

    internal class MstatSmsMessage
    {
        /// <summary>Defines the Sender ID used for this channel. According to the SMS specification the Sender ID can be either Numeric or Alphanumeric.</summary>
        /// <value>
        /// Numeric: Up to 15 numbers with an optional plus sign (ex.+1555999999).
        /// <br/>
        /// Alphanumeric: Up to 11 Latin characters or numbers. 
        /// <br/>
        /// Simple symbols are supported but may cause deliverability issues to various international operators. 
        /// Safe to Use Symbols are the following: + - _ ! $ ., and space.
        /// </value>
        [JsonPropertyName("from")]
        public string? From { get; set; }

        /// <summary>The text that the recipient received as an SMS message. Multipart/Long SMS messages are supported.</summary>
        /// <value>
        /// You can find more information about the characters supported as well as the character limits in Documentation: M-STAT_TMS_Fallback_1.5 [Appendices 10.2 and 10.3].
        /// </value>
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        public MstatSmsMessage() {

        }
        public MstatSmsMessage(string from, string? message) {
            From = from;
            Text = message;
        }
    }

    internal class MstatResponse
    {
        /// <summary>Request Status</summary>
        /// <value>
        /// The standard response format for successful requests excluding HTTP 204 No Content is: "success". 
        /// <br/>
        /// The standard response format for failed requests is: "error".
        /// </value>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("data")]
        public JsonElement Data { get; set; }

        public bool IsStatusSuccess() {
            return Status == "success";
        }

        public class ResponseData
        {

            [JsonPropertyName("channel")]
            public string? Channel { get; set; }

            [JsonPropertyName("destination")]
            public string? Destination { get; set; }

            [JsonPropertyName("unique_id")]
            public string UniqueId { get; set; } = null!;

            [JsonPropertyName("flow")]
            public List<MstatFlow>? Flows { get; set; }

            [JsonPropertyName("date")]
            public string? Date { get; set; }

            [JsonPropertyName("status")]
            public MstatResponseStatus? Status { get; set; }

            [JsonPropertyName("errors")]
            public List<string> Errors { get; set; } = [];

            [JsonIgnore]
            public bool HasErrros => Errors?.Count > 0;
        }

        public class MstatResponseStatus
        {
            /// <summary>Source of the status. It used for Reports</summary>
            /// <example>tms, viber, sms</example>
            /// <value>Check the values in the Documentation: M-STAT_TMS_Fallback_1.5 [Appendices 10.1]</value>
            [JsonPropertyName("source")]
            public string? Source { get; set; }

            /// <summary>The status of the message.</summary>
            /// <value>Check the values in the Documentation: M-STAT_TMS_Fallback_1.5 [Appendices 10.1]</value>
            [JsonPropertyName("code")]
            public string? Code { get; set; }

            /// <summary>The description of the status code.</summary>
            /// <value>Check the values in the Documentation: M-STAT_TMS_Fallback_1.5 [Appendices 10.1]</value>
            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("date")]
            public string? Date { get; set; }
        }
    }

    /// <summary> Each Viber message has a specific message type. The message type defines the target devices of the Viber message as well as the message content combination that is used.</summary>
    public enum ViberMessageType
    {
        /// <summary> </summary>
        OneWayTextSmartphones = 6,
        /// <summary> </summary>
        OneWayTextSmartphonesTablets = 106,
        /// <summary> </summary>
        TwoWayTextSmartphonesTablets = 206,
        /// <summary> </summary>
        OneWayImageSmartphones = 7,
        /// <summary> </summary>
        OneWayImageSmartphonesTablets = 107,
        /// <summary> </summary>
        TwoWayImageSmartphonesTablets = 207,
        /// <summary> </summary>
        OneWayTextImageButtonSmartphones = 8,
        /// <summary> </summary>
        OneWayTextImageButtonSmartphonesTablets = 108,
        /// <summary> </summary>
        TwoWayTextImageButtonSmartphonesTablets = 208,
        /// <summary> </summary>
        OneWayTextButtonSmartphones = 9,
        /// <summary> </summary>
        OneWayTextButtonSmartphonesTablets = 109,
        /// <summary> </summary>
        TwoWayTextButtonSmartphonesTablets = 209,
    }
}

/// <summary>Extra settings class for configuring Mstat SMS service client. </summary>
public class SmsServiceMstatSettings : SmsServiceSettings
{
    /// <summary>Sender ID that a client is allowed to use when sending Viber ServiceMessages</summary>
    public string? Channel { get => Sender; set => Sender = value; }
    /// <summary>Each message submitted to the TMS platform must contain a uniqueidentifier. 
    /// <br/>
    /// This identifier is used to prevent duplicate message submissions to the service and to link delivery reports with the appropriate messages</summary>
    public bool GenerateUniqueId { get; set; }
}



