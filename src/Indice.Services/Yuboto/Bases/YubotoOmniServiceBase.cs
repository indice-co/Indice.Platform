using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Indice.Services.Yuboto.Bases
{
    /// <summary>
    /// Yuboto Service for the new Omni API responsible to send SMS/Viber messages
    /// </summary>
    public class YubotoOmniServiceBase
    {
        private bool _disposed = false;

        /// <summary>
        /// The settings required to configure the service.
        /// </summary>
        protected SmsServiceSettings Settings { get; }

        /// <summary>
        /// The <see cref="System.Net.Http.HttpClient"/>.
        /// </summary>
        protected HttpClient HttpClient { get; }

        /// <summary>
        /// Represents a type used to perform logging.
        /// </summary>
        protected ILogger<YubotoOmniServiceBase> Logger { get; }

        /// <summary>
        /// Constructs the <see cref="YubotoOmniServiceBase"/> using the <seealso cref="SmsServiceSettings"/>.
        /// </summary>
        /// <param name="settings">The settings required to configure the service.</param>
        /// <param name="httpClient">Injected <see cref="System.Net.Http.HttpClient"/> managed by the DI.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public YubotoOmniServiceBase(HttpClient httpClient, SmsServiceSettings settings, ILogger<YubotoOmniServiceBase> logger) {
            HttpClient = httpClient ?? new HttpClient();
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (HttpClient.BaseAddress == null) {
                HttpClient.BaseAddress = new Uri("https://services.yuboto.com/omni/v1/");
            }
            if (!HttpClient.DefaultRequestHeaders.Contains("Authorization")) {
                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(Settings.ApiKey)));
            }
        }

        #region Helper Methods

        /// <summary>
        /// Get list of phone numbers from destination
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        protected string[] GetRecipientsFromDestination(string destination) {
            var recipients = (destination ?? string.Empty).Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            if (recipients == null) {
                throw new ArgumentNullException(nameof(recipients));
            }

            if (recipients.Length == 0) {
                throw new ArgumentException("Recipients list is empty.", nameof(recipients));
            }

            // Yuboto doc: Use country code without + or 00
            recipients = recipients.Select(phoneNumber => {
                if (phoneNumber.StartsWith("+")) {
                    return phoneNumber.Substring(1);
                } else if (phoneNumber.StartsWith("00")) {
                    return phoneNumber.Substring(2);
                }
                return phoneNumber;
            }).ToArray();

            if (recipients.Any(telephone => telephone.Any(character => !char.IsNumber(character)))) {
                throw new ArgumentException("Invalid recipients. Recipients cannot contain letters.", nameof(recipients));
            }
            return recipients;
        }

        /// <summary>
        /// Get default Json Serializer Options
        /// CamelCase, ignore null values
        /// </summary>
        /// <returns></returns>
        protected JsonSerializerOptions GetJsonSerializerOptions() {
            return new JsonSerializerOptions {
                IgnoreNullValues = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        #endregion

        /// <summary>
        /// Disposes the <see cref="System.Net.Http.HttpClient"/> if not managed by the DI.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose
        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"></param>
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

        /// <summary>
        /// The request body is of type SendRequest.
        /// </summary>
        public class SendRequest
        {
            private SendRequest() {

            }

            /// <summary>
            /// Create a Send SMS request 
            /// </summary>
            /// <param name="phoneNumbers"></param>
            /// <param name="sender"></param>
            /// <param name="message"></param>
            /// <returns></returns>
            public static SendRequest CreateSms(string[] phoneNumbers, string sender, string message) {
                return new SendRequest {
                    PhoneNumbers = phoneNumbers,
                    Sms = new SmsObj {
                        Sender = sender,
                        Text = message,
                        TypeSms = "sms",
                        LongSms = message.Length > 160
                    }
                };
            }

            /// <summary>
            ///  Create a Send Viber request
            /// </summary>
            /// <param name="phoneNumbers"></param>
            /// <param name="sender"></param>
            /// <param name="message"></param>
            /// <param name="expiredMessage"></param>
            /// <returns></returns>
            public static SendRequest CreateViber(string[] phoneNumbers, string sender, string message, string expiredMessage = "Message is expired.") {
                return new SendRequest {
                    PhoneNumbers = phoneNumbers,
                    Viber = new ViberObj {
                        Sender = sender,
                        Text = message,
                        ExpiryText = expiredMessage
                    }
                };
            }

            /// <summary>
            /// Refers to the phone number of the recipient or recipients of the text message (use array for multiple recipients).
            /// Required 
            /// Use country code without + or 00.
            /// </summary>
            [JsonPropertyName("phonenumbers")]
            public string[] PhoneNumbers { get; set; }

            /// <summary>
            /// Indicates the date you wish to send the message. If this is omitted, the message is sent instantly.
            /// Not Required
            /// ΥΥΥΥΜΜDD 
            /// YYYY refers to the year ΜΜ refers to the month DD refers to the day
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

            /// <summary>
            /// The flag indicates if delivery receipt request must be sent to customer’s application. (Default: false)
            /// </summary>
            [JsonPropertyName("dlr")]
            public bool Dlr { get; set; } = false;

            /// <summary>
            /// When the message reaches its final state, a call to this url will be performed by Yuboto’s system with the message’s delivery info.
            /// You can add a persistent callback url in your account without sending a callbackURL parameter each time you call the API.Contact with support @yuboto.com in order to set your persistent callbackURL under your account.
            /// Not Required
            /// </summary>
            [JsonPropertyName("callbackUrl")]
            public string CallbackUrl { get; set; }

            /// <summary>
            /// User defined value that will be included in the call to the provided callback_url
            /// Option1 and Option2 Parameters will be available for retrieve only if you pass dlr:true and a callbackUrl parameter
            /// Not Required
            /// </summary>
            [JsonPropertyName("option1")]
            public string Option1 { get; set; }

            /// <summary>
            /// User defined value that will be included in the call to the provided callback_url.
            /// Option1 and Option2 Parameters will be available for retrieve only if you pass dlr:true and a callbackUrl parameter
            /// Not Required
            /// </summary>
            [JsonPropertyName("option2")]
            public string Option2 { get; set; }

            /// <summary>
            /// This object is required if list of channels contains SMS channel.
            /// One of Sms/Viber parameters must always exists.
            /// Not Required
            /// <see cref="SmsObj"/>
            /// </summary>
            [JsonPropertyName("sms")]
            public SmsObj Sms { get; set; }

            /// <summary>
            /// This object is required if a list of channels contains VIBER channel. Parameters text, buttonCaption + buttonAction and image make Viber Service Message content. There are 4 possible combinations of Viber Service Message content: text only, image only, text + button, text + button + image
            /// One of Sms/Viber parameters must always exists.
            /// Not Required
            /// <see cref="ViberObj"/>
            /// </summary>
            [JsonPropertyName("viber")]
            public ViberObj Viber { get; set; }

            /// <summary>
            /// The Sms Object of SendRequest
            /// </summary>
            public class SmsObj
            {
                /// <summary>
                /// SMS originator (“sender”) that will be displayed on mobile device’s screen. Alphanumeric origin, max. 11 characters | Numeric origin, max. 20 characters
                /// Required
                /// </summary>
                [JsonPropertyName("sender")]
                public string Sender { get; set; }

                /// <summary>
                /// The text of the message. If two factor authentication is activated TwoFa, is mandatory that ‘{pin_code}’ is included in this string. This placeholder will then be replaced with the generated pin.
                /// Required
                /// </summary>
                [JsonPropertyName("text")]
                public string Text { get; set; }

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
                /// Indicates which channel has priority when it comes to omni messaging (default value is: 0)
                /// Not Required
                /// </summary>
                [JsonPropertyName("priority")]
                public int Priority { get; set; } = 0;

                /// <summary>
                /// If Two Factor Authentication is needed then provide this object along with other values.
                /// <see cref="TwoFaObj"/>
                /// </summary>
                [JsonPropertyName("TwoFa")]
                public TwoFaObj TwoFa { get; set; }
            }

            /// <summary>
            /// The Viber Object of SendRequest
            /// </summary>
            public class ViberObj
            {
                /// <summary>
                /// Viber message originator (“sender”) that will be displayed on mobile device’s screen. Alphanumeric origin, max. 20 characters
                /// Required
                /// </summary>
                [JsonPropertyName("sender")]
                public string Sender { get; set; }

                /// <summary>
                /// The Viber Service Message text. Text length can be up to 1000 characters. VIBER text can be sent alone, without button or image.
                /// If two factor authentication is activated TwoFa, is mandatory that ‘{pin_code}’ is included in this string. This placeholder will then be replaced with the generated pin.
                /// Not Required
                /// </summary>
                [JsonPropertyName("text")]
                public string Text { get; set; }

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
                public string ExpiryText { get; set; }

                /// <summary>
                /// A textual writing on the button. Maximum length is 30 characters. The VIBER button can be sent only if Viber Service Message contains text.
                /// Not Required
                /// </summary>
                [JsonPropertyName("buttonCaption")]
                public string ButtonCaption { get; set; }

                /// <summary>
                /// The link of button action.
                /// Not Required
                /// </summary>
                [JsonPropertyName("buttonAction")]
                public string ButtonAction { get; set; }

                /// <summary>
                /// The URL address of image sent to end user. The VIBER image can be sent only alone or together with text and button.
                /// Not Required
                /// </summary>
                [JsonPropertyName("image")]
                public string Image { get; set; }

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
                public TwoFaObj TwoFa { get; set; }
            }

            /// <summary>
            /// The Two Factor Object of ViberObj | SmsObj
            /// </summary>
            public class TwoFaObj
            {
                /// <summary>
                /// The length of the pin to be generated Min:4 Max: 32
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
                public string PinType { get; set; }

                /// <summary>
                /// Whether the pin should be case sensitive.(alpha, alphanumeric) If false, the case sensitivity would not be checked when validating, 
                /// if true, code for validation needs to be entered exactly as provided.
                /// Required
                /// </summary>
                [JsonPropertyName("isCaseSensitive")]
                public bool IsCaseSensitive { get; set; }

                /// <summary>
                /// The time the pin will be active. Accepted values between 60-600 (in seconds)
                /// Required
                /// </summary>
                [JsonPropertyName("expiration")]
                public int Expiration { get; set; }
            }
        }

        /// <summary>
        /// The SendRequest response 
        /// </summary>
        public class SendResponse
        {
            /// <summary>
            /// The response error code for this call. This will be 0 if successful.
            /// </summary>
            [JsonPropertyName("ErrorCode")]
            public int ErrorCode { get; set; }

            /// <summary>
            /// The response error message. This will be null if successful.
            /// </summary>
            [JsonPropertyName("ErrorMessage")]
            public string ErrorMessage { get; set; }

            /// <summary>
            /// A list which contains the status of the messages. <see cref="MessageStatus"/>
            /// </summary>
            [JsonPropertyName("Message")]
            public List<MessageStatus> Messages { get; set; }

            /// <summary>
            /// Check if the request succeded.
            /// </summary>
            public bool IsSuccess => ErrorCode == 0;

            /// <summary>
            /// MessageStatus model
            /// </summary>
            public class MessageStatus
            {
                /// <summary>
                /// The id of message status.
                /// </summary>
                [JsonPropertyName("id")]
                public string Id { get; set; }

                /// <summary>
                /// The channel that the message will be send (SMS or Viber).
                /// </summary>
                [JsonPropertyName("channel")]
                public string Channel { get; set; }

                /// <summary>
                /// Refers to the phone number of the recipient of the text message.
                /// </summary>
                [JsonPropertyName("phonenumber")]
                public string PhoneNumber { get; set; }

                /// <summary>
                /// The status of the message.
                /// </summary>
                [JsonPropertyName("status")]
                public string Status { get; set; }
            }
        }

        #endregion
    }
}
