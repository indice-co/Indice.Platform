using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Indice.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Indice.Services;

/// <summary>Yuboto Service for the new Omni API responsible to send SMS/Viber messages</summary>
public abstract class SmsServiceYubotoOmniBase
{
    private bool _disposed = false;

    /// <summary>Constructs the <see cref="SmsServiceYubotoOmniBase"/> using the <seealso cref="SmsServiceSettings"/>.</summary>
    /// <param name="settings">The settings required to configure the service.</param>
    /// <param name="httpClient">Injected <see cref="System.Net.Http.HttpClient"/> managed by the DI.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public SmsServiceYubotoOmniBase(
        HttpClient httpClient, 
        IOptionsSnapshot<SmsServiceYubotoOmniSettings> settings, 
        ILogger<SmsServiceYubotoOmniBase> logger
    ) {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (HttpClient.BaseAddress == null) {
            HttpClient.BaseAddress = new Uri("https://services.yuboto.com/omni/v1/");
        }
        if (!HttpClient.DefaultRequestHeaders.Contains("Authorization")) {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(Settings.ApiKey!)));
        }
    }

    /// <summary>The settings required to configure the service.</summary>
    protected SmsServiceYubotoOmniSettings Settings { get; }
    /// <summary>The <see cref="System.Net.Http.HttpClient"/>.</summary>
    protected HttpClient HttpClient { get; }
    /// <summary>Represents a type used to perform logging.</summary>
    protected ILogger<SmsServiceYubotoOmniBase> Logger { get; }

    #region Helper Methods
    /// <summary>Get list of phone numbers from destination.</summary>
    /// <param name="destination"></param>
    protected string[] GetRecipientsFromDestination(string destination) {
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
        // Yuboto doc: Use country code without + or 00
        //recipients = recipients.Select(phoneNumber => {
        //    if (phoneNumber.StartsWith("+")) {
        //        return phoneNumber[1..];
        //    } else if (phoneNumber.StartsWith("00")) {
        //        return phoneNumber[2..];
        //    }
        //    return phoneNumber;
        //})
        //.ToArray();
        if (recipients.Any(telephone => telephone.Any(character => !char.IsNumber(character)))) {
            throw new ArgumentException("Invalid recipients. Recipients cannot contain letters.", nameof(recipients));
        }
        return recipients;
    }

    /// <summary>Get default Json Serializer Options: CamelCase, ignore null values.</summary>
    protected JsonSerializerOptions GetJsonSerializerOptions() => new JsonSerializerOptions {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    #endregion

    /// <summary>Disposes the <see cref="System.Net.Http.HttpClient"/> if not managed by the DI.</summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Protected implementation of Dispose pattern.</summary>
    /// <param name="disposing">To detect redundant calls.</param>
    /// <example>https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose</example>
    protected virtual void Dispose(bool disposing) {
        if (_disposed) {
            return;
        }
        if (disposing) {
            // Free any other managed objects here.
            HttpClient.Dispose();
        }
        // Free any unmanaged objects here.
        _disposed = true;
    }

    #region Models
    internal class SendRequest
    {
        public static SendRequest CreateSms(string[] phoneNumbers, string sender, string message, int validity, string? callbackUrl = null, bool dlr = false) => new SendRequest {
            PhoneNumbers = phoneNumbers,
            Sms = new SmsObj {
                Sender = sender,
                Text = message,
                TypeSms = "sms",
                LongSms = message.Length > 160,
                Validity = validity
            },
            CallbackUrl = callbackUrl,
            Dlr = dlr
        };

        public static SendRequest CreateViber(string[] phoneNumbers, string sender, string message, bool viberFallbackEnabled, int validity, string expiredMessage = "Message is expired.", string? callbackUrl = null, bool dlr = false) => new SendRequest {
            PhoneNumbers = phoneNumbers,
            Viber = new ViberObj {
                Sender = sender,
                Text = message,
                ExpiryText = expiredMessage,
                Priority = 0,
                Validity = validity
            },
            Sms = viberFallbackEnabled ? new SmsObj {
                Sender = sender,
                Text = message,
                TypeSms = "sms",
                LongSms = message.Length > 160,
                Priority = 1,
                Validity = validity
            } : default,
            CallbackUrl = callbackUrl,
            Dlr = dlr
        };

        /// <summary>
        /// Refers to the phone number of the recipient or recipients of the text message (use array for multiple recipients).
        /// Required 
        /// Use country code without + or 00.
        /// </summary>
        [JsonPropertyName("phonenumbers")]
        public string[] PhoneNumbers { get; set; } = [];
        /// <summary>
        /// Indicates the date you wish to send the message. If this is omitted, the message is sent instantly.
        /// Not Required
        /// ΥΥΥΥΜΜDD 
        /// YYYY refers to the year ΜΜ refers to the month DD refers to the day.
        /// </summary>
        [JsonPropertyName("dateinToSend")]
        public int? DateInToSend { get; set; }
        /// <summary>
        /// Indicates the time you wish to send your message. If this is omitted, the message is sent instantly.
        /// Not Required
        /// HHMM
        /// ΗΗ refers to the hour ΜΜ refers to minutes
        /// </summary>
        [JsonPropertyName("timeinToSend")]
        public int? TimeInToSend { get; set; }
        /// <summary>The flag indicates if delivery receipt request must be sent to customer’s application. (Default: false).</summary>
        [JsonPropertyName("dlr")]
        public bool Dlr { get; set; } = false;
        /// <summary>
        /// When the message reaches its final state, a call to this url will be performed by Yuboto’s system with the message’s delivery info.
        /// You can add a persistent callback url in your account without sending a callbackURL parameter each time you call the API.Contact with support @yuboto.com in order to set your persistent callbackURL under your account.
        /// Not Required
        /// </summary>
        [JsonPropertyName("callbackUrl")]
        public string? CallbackUrl { get; set; }
        /// <summary>
        /// User defined value that will be included in the call to the provided callback_url.
        /// Option1 and Option2 Parameters will be available for retrieve only if you pass dlr:true and a callbackUrl parameter.
        /// Not Required
        /// </summary>
        [JsonPropertyName("option1")]
        public string? Option1 { get; set; }
        /// <summary>
        /// User defined value that will be included in the call to the provided callback_url.
        /// Option1 and Option2 Parameters will be available for retrieve only if you pass dlr:true and a callbackUrl parameter.
        /// Not Required
        /// </summary>
        [JsonPropertyName("option2")]
        public string? Option2 { get; set; }
        /// <summary>
        /// This object is required if list of channels contains SMS channel.
        /// One of Sms/Viber parameters must always exists.
        /// Not Required
        /// <see cref="SmsObj"/>
        /// </summary>
        [JsonPropertyName("sms")]
        public SmsObj? Sms { get; set; }
        /// <summary>
        /// This object is required if a list of channels contains VIBER channel. Parameters text, buttonCaption + buttonAction and image make Viber Service Message content. 
        /// There are 4 possible combinations of Viber Service Message content: text only, image only, text + button, text + button + image.
        /// One of Sms/Viber parameters must always exists.
        /// Not Required
        /// <see cref="ViberObj"/>
        /// </summary>
        [JsonPropertyName("viber")]
        public ViberObj? Viber { get; set; }

        /// <summary>The Sms Object of SendRequest.</summary>
        public class SmsObj
        {
            /// <summary>
            /// SMS originator (“sender”) that will be displayed on mobile device’s screen. Alphanumeric origin, max. 11 characters | Numeric origin, max. 20 characters.
            /// Required
            /// </summary>
            [JsonPropertyName("sender")]
            public string? Sender { get; set; }
            /// <summary>
            /// The text of the message. If two factor authentication is activated TwoFa, is mandatory that ‘{pin_code}’ is included in this string. This placeholder will then be replaced with the generated pin.
            /// Required
            /// </summary>
            [JsonPropertyName("text")]
            public string? Text { get; set; }
            /// <summary>
            /// If the SMS is not delivered directly, this variable indicates the amount of seconds for which the message will remain active, before being rejected by the SMSC.
            /// Min Value: 30
            /// Max Value: 4320 (default)
            /// Not Required
            /// </summary>
            [JsonPropertyName("validity")]
            public int Validity { get; set; } = 4320;
            /// <summary>
            /// Indicates the type of message you wish to send.
            /// Accepted values: 
            /// 1. sms (default) 
            /// 2. flash 
            /// 3. unicode
            /// Not Required
            /// </summary>
            [JsonPropertyName("typesms")]
            public string TypeSms { get; set; } = "sms";
            /// <summary>
            /// Indicates if the message can be over 160 characters. It applies only to standard type SMS.
            /// 1. false (default) 
            /// 2. true
            /// Not Required
            /// </summary>
            [JsonPropertyName("longsms")]
            public bool LongSms { get; set; } = false;
            /// <summary>
            /// Indicates which channel has priority when it comes to omni messaging (default value is: 0).
            /// Not Required
            /// </summary>
            [JsonPropertyName("priority")]
            public int Priority { get; set; } = 0;
            /// <summary>
            /// If Two Factor Authentication is needed then provide this object along with other values.
            /// <see cref="TwoFaObj"/>
            /// </summary>
            [JsonPropertyName("TwoFa")]
            public TwoFaObj? TwoFa { get; set; }

            /// <summary>
            /// Callback URL that Yuboto invokes with delivery details when the message reaches a final state.
            /// Not Required
            /// </summary>
            [JsonPropertyName("CallbackUrl")]
            public string? CallbackUrl { get; set; }

            /// <summary>
            /// Indicates whether delivery receipt notifications are requested for this message.
            /// Not Required
            /// </summary>
            [JsonPropertyName("Dlr")]
            public bool? Dlr { get; set; }
        }

        /// <summary>The Viber Object of SendRequest</summary>
        public class ViberObj
        {
            /// <summary>
            /// Viber message originator (“sender”) that will be displayed on mobile device’s screen. Alphanumeric origin, max. 20 characters.
            /// Required
            /// </summary>
            [JsonPropertyName("sender")]
            public string? Sender { get; set; }
            /// <summary>
            /// The Viber Service Message text. Text length can be up to 1000 characters. VIBER text can be sent alone, without button or image.
            /// If two factor authentication is activated TwoFa, is mandatory that ‘{pin_code}’ is included in this string. This placeholder will then be replaced with the generated pin.
            /// Not Required
            /// </summary>
            [JsonPropertyName("text")]
            public string? Text { get; set; }
            /// <summary>
            /// If the Viber message is not delivered directly, this variable indicates the amount of seconds for which the message will remain active, before being rejected.
            /// Min Value: 15 
            /// Max Value:86.400 (default)
            /// Not Required
            /// </summary>
            [JsonPropertyName("validity")]
            public int Validity { get; set; } = 86400;
            /// <summary>
            /// Relevant for iOS version of Viber application (iPhone users only). This is the text that will be displayed if Viber Service Message expires.
            /// Required
            /// </summary>
            [JsonPropertyName("expiryText")]
            public string? ExpiryText { get; set; }
            /// <summary>
            /// A textual writing on the button. Maximum length is 30 characters. The VIBER button can be sent only if Viber Service Message contains text.
            /// Not Required
            /// </summary>
            [JsonPropertyName("buttonCaption")]
            public string? ButtonCaption { get; set; }
            /// <summary>
            /// The link of button action.
            /// Not Required
            /// </summary>
            [JsonPropertyName("buttonAction")]
            public string? ButtonAction { get; set; }
            /// <summary>
            /// The URL address of image sent to end user. The VIBER image can be sent only alone or together with text and button.
            /// Not Required
            /// </summary>
            [JsonPropertyName("image")]
            public string? Image { get; set; }
            /// <summary>
            /// Indicates which channel has priority when it comes to omni messaging (default value is: 0).
            /// Not Reuqired
            /// </summary>
            [JsonPropertyName("priority")]
            public int Priority { get; set; }
            /// <summary>
            /// If Two Factor Authentication is needed then provide this object along with other values.
            /// <see cref="TwoFaObj"/>
            /// </summary>
            [JsonPropertyName("TwoFa")]
            public TwoFaObj? TwoFa { get; set; }

            /// <summary>
            /// Callback URL that Yuboto invokes with delivery details when the message reaches a final state.
            /// Not Required
            /// </summary>
            [JsonPropertyName("CallbackUrl")]
            public string? CallbackUrl { get; set; }

            /// <summary>
            /// Indicates whether delivery receipt notifications are requested for this message.
            /// Not Required
            /// </summary>
            [JsonPropertyName("Dlr")]
            public bool? Dlr { get; set; }
        }

        /// <summary>The Two Factor Object of ViberObj | SmsObj.</summary>
        public class TwoFaObj
        {
            /// <summary>
            /// The length of the pin to be generated Min:4 Max: 32.
            /// Required
            /// </summary>
            [JsonPropertyName("pinLength")]
            public int PinLength { get; set; }
            /// <summary>
            /// Accepted values: 
            /// 1. ALPHA (PQRST)
            /// 2. ALPHA_ALPHA_LOWER_NUMERIC (Pg3Gh) 
            /// 3. ALPHA_NUMERIC (hEQsa) 
            /// 4. NUMERICWITHOUTZERO (5443) 
            /// 5. NUMERIC (54034)
            /// Required
            /// </summary>
            [JsonPropertyName("pinType")]
            public string? PinType { get; set; }
            /// <summary>
            /// Whether the pin should be case sensitive.(alpha, alphanumeric) If false, the case sensitivity would not be checked when validating, 
            /// if true, code for validation needs to be entered exactly as provided.
            /// Required
            /// </summary>
            [JsonPropertyName("isCaseSensitive")]
            public bool IsCaseSensitive { get; set; }
            /// <summary>
            /// The time the pin will be active. Accepted values between 60-600 (in seconds).
            /// Required
            /// </summary>
            [JsonPropertyName("expiration")]
            public int Expiration { get; set; }
        }
    }

    internal class SendResponse
    {
        /// <summary>The response error code for this call. This will be 0 if successful.</summary>
        [JsonPropertyName("ErrorCode")]
        public int ErrorCode { get; set; }
        /// <summary>The response error message. This will be null if successful.</summary>
        [JsonPropertyName("ErrorMessage")]
        public string? ErrorMessage { get; set; }
        /// <summary>A list which contains the status of the messages. <see cref="MessageStatus"/>.</summary>
        [JsonPropertyName("Message")]
        public List<MessageStatus> Messages { get; set; } = [];
        /// <summary>Check if the request succeded.</summary>
        public bool IsSuccess => ErrorCode == 0;

        /// <summary>MessageStatus model.</summary>
        public class MessageStatus
        {
            /// <summary>The id of message status.</summary>
            [JsonPropertyName("id")]
            public string? Id { get; set; }
            /// <summary>The channel that the message will be send (SMS or Viber).</summary>
            [JsonPropertyName("channel")]
            public string? Channel { get; set; }
            /// <summary>Refers to the phone number of the recipient of the text message.</summary>
            [JsonPropertyName("phonenumber")]
            public string? PhoneNumber { get; set; }
            /// <summary>The status of the message.</summary>
            [JsonPropertyName("status")]
            public string? Status { get; set; }
        }
    }
    #endregion
}

/// <summary>Extra settings class for configuring Yuboto Omni SMS service client.</summary>
public class SmsServiceYubotoOmniSettings : SmsServiceSettings
{
    /// <summary>The optional secret to be used for producing the callback URL signature.</summary>
    public string? WebhookSecret { get; set; } = string.Empty;
}

/// <inheritdoc/>
public sealed class YubotoWebhookHttpHandler : IWebhookHttpHandler {
    private readonly SmsServiceYubotoOmniSettings _settings;

    /// <inheritdoc/>
    public YubotoWebhookHttpHandler(IOptionsSnapshot<SmsServiceYubotoOmniSettings> settings) {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc/>
    public async Task<WebhookHttpResult> ParseAsync(
        IDictionary<string, StringValues> requestHeaders,
        IEnumerable<KeyValuePair<string, StringValues>> requestParameters,
        Stream requestBody,
        CancellationToken cancellationToken) {
        if (IsSignatureInvalid(requestParameters)) {
            return WebhookHttpResult.BadRequest();
        }

        var parameters = requestParameters.ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);

        var messageId = parameters.GetValueOrDefault("smsid").ToString();
        var recipient = parameters.GetValueOrDefault("receiver").ToString();
        var sender = parameters.GetValueOrDefault("sender").ToString();
        var status = parameters.GetValueOrDefault("status").ToString();
        var statusDescription = parameters.GetValueOrDefault("statusDescription").ToString();
        var channel = parameters.GetValueOrDefault("channel").ToString();
        var dlrDate = parameters.GetValueOrDefault("dlrDate").ToString();

        if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(recipient)) {
            return WebhookHttpResult.BadRequest("Yuboto webhook requires 'smsid' and 'receiver' query parameters.");
        }

        var timestamp = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(dlrDate) && !DateTimeOffset.TryParse(dlrDate, out timestamp)) {
            return WebhookHttpResult.BadRequest("Invalid 'dlrDate' value in Yuboto webhook payload.");
        }

        var payload = new JsonObject();
        foreach (var parameter in parameters) {
            payload[parameter.Key] = parameter.Value.ToString();
        }
        var eventid = !string.IsNullOrWhiteSpace(statusDescription) ? $"{messageId}:{statusDescription}:{timestamp}" : $"{messageId}:{timestamp}";
        var eventType = "";
        if (!string.IsNullOrWhiteSpace(statusDescription)) {
            eventType = statusDescription;
        } else if (!string.IsNullOrWhiteSpace(status)) {
            eventType = $"Status:{status}";
        } else if (!string.IsNullOrWhiteSpace(channel)) {
            eventType = $"{channel}Delivery";
        }

        var incomingEvents = new List<WebhookEvent> {
            new() {
                Provider = "Yuboto",
                EventType = eventType,
                EventId = eventid,
                MessageId = messageId,
                Recipient = recipient,
                Subject = sender,
                Timestamp = timestamp,
                Payload = payload
            }
        };

        await Task.CompletedTask;
        return WebhookHttpResult.Ok(incomingEvents);
    }

    private bool IsSignatureInvalid(IEnumerable<KeyValuePair<string, StringValues>> requestParameters) {
        if (string.IsNullOrWhiteSpace(_settings.WebhookSecret)) {
            return false;
        }

        var parameters = requestParameters.ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);
        var hmacToken = parameters.GetValueOrDefault("option1").ToString();
        var option2 = parameters.GetValueOrDefault("option2").ToString();
        var reference = string.Empty;
        var timestamp = string.Empty;

        if (!string.IsNullOrWhiteSpace(option2)) {
            var separator = option2.IndexOf('_');
            if (separator > 0 && separator < option2.Length - 1) {
                var parts = option2.Split('_');
                reference = parts[0];
                timestamp = parts[1];
            }
        }

        if (string.IsNullOrWhiteSpace(hmacToken) ||
            string.IsNullOrWhiteSpace(reference) ||
            string.IsNullOrWhiteSpace(timestamp)) {
            return true;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.WebhookSecret));
        var expectedHmac = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{reference}:{timestamp}"));

        try {
            if (TryDecodeCompactToken(hmacToken, out var compactToken)) {
                if (compactToken.Length == expectedHmac.Length) {
                    return !CryptographicOperations.FixedTimeEquals(compactToken, expectedHmac);
                }
                return true;
            }
            return !CryptographicOperations.FixedTimeEquals(Convert.FromHexString(hmacToken), expectedHmac);
        } catch (FormatException) {
            return true;
        } catch (ArgumentException) {
            return true;
        }
    }

    private static bool TryDecodeCompactToken(string token, out byte[] bytes) {
        bytes = [];
        if (string.IsNullOrWhiteSpace(token)) {
            return false;
        }

        var base64 = token.Replace('-', '+').Replace('_', '/');
        var remainder = base64.Length % 4;
        if (remainder != 0) {
            base64 = base64.PadRight(base64.Length + (4 - remainder), '=');
        }

        try {
            bytes = Convert.FromBase64String(base64);
            return true;
        } catch (FormatException) {
            return false;
        } catch (ArgumentException) {
            return false;
        }
    }
}

internal static class YubotoSmsServiceExtensions {
    private const int OPTION1_MAX_LENGTH = 50;

    /// <summary>Generates a callback URL with a reference identifier for a Yuboto request.</summary>
    public static TRequest SetCallBackSignature<TRequest>(this TRequest request, string secretKey) where TRequest : SmsServiceYubotoOmniBase.SendRequest {
        if (string.IsNullOrWhiteSpace(secretKey)) {
            return request;
        }
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        var referenceId = Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
        var payload = $"{referenceId}:{timestamp}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var signature = Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var option1 = signature;
        request.Option1 = option1;
        var option2 = payload.Replace(":", "_");
        request.Option2 = option2;
        return request;
    }
}
