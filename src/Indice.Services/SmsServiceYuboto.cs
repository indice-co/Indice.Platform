using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Indice.Services
{
    /// <summary>Sms Service implementation using the Yuboto sms service gateway.</summary>
    public class SmsServiceYuboto : ISmsService, IDisposable
    {
        private bool _disposed = false;

        /// <summary>The settings required to configure the service.</summary>
        protected SmsServiceSettings Settings { get; }
        /// <summary>The <see cref="System.Net.Http.HttpClient"/>.</summary>
        protected HttpClient HttpClient { get; }
        /// <summary>Represents a type used to perform logging.</summary>
        protected ILogger<SmsServiceYuboto> Logger { get; }

        /// <summary>Constructs the <see cref="SmsServiceYuboto"/> using the <seealso cref="SmsServiceSettings"/>.</summary>
        /// <param name="settings">The settings required to configure the service.</param>
        /// <param name="httpClient">Injected <see cref="System.Net.Http.HttpClient"/> managed by the DI.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public SmsServiceYuboto(HttpClient httpClient, SmsServiceSettings settings, ILogger<SmsServiceYuboto> logger) {
            HttpClient = httpClient ?? new HttpClient();
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (HttpClient.BaseAddress == null) {
                var uri = "https://services.yuboto.com/web2sms/api/v2/smsc.aspx";
                HttpClient.BaseAddress = new Uri(uri);
            }
        }

        /// <inheritdoc/>
        public async Task SendAsync(string destination, string subject, string body, SmsSender sender = null) {
            HttpResponseMessage httpResponse;
            YubotoResponse response;
            var recipients = (destination ?? string.Empty).Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            if (recipients == null) {
                throw new ArgumentNullException(nameof(recipients));
            }
            if (recipients.Length == 0) {
                throw new ArgumentException("Recipients list is empty.", nameof(recipients));
            }
            if (recipients.Any(telephone => telephone.Any(character => !char.IsNumber(character)))) {
                throw new ArgumentException("Invalid recipients. Recipients cannot contain letters.", nameof(recipients));
            }
            var request = new YubotoRequestHelper(Settings.ApiKey, sender?.Id ?? Settings.Sender ?? Settings.SenderName, recipients, body) {
                IsSmsTest = Settings.TestMode
            }
            .CreateRequest();
            httpResponse = await HttpClient.GetAsync(request);
            var stringifyResponse = await httpResponse.Content.ReadAsStringAsync();
            response = JsonConvert.DeserializeObject<YubotoResponse>(stringifyResponse);
            if (response.HasError) {
                throw new SmsServiceException($"SMS Delivery failed. {response}");
            } else {
                Logger?.LogInformation("SMS message successfully sent: {1}", response.OK.FirstOrDefault());
            }
        }

        /// <summary>Disposes the <see cref="System.Net.Http.HttpClient"/> if not managed by the DI.</summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose
        /// <summary>Protected implementation of Dispose pattern.</summary>
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

        /// <summary>Checks the implementation if supports the given <paramref name="deliveryChannel"/>.</summary>
        /// <param name="deliveryChannel">A string representing the delivery channel. i.e 'SMS'</param>
        /// <returns></returns>
        public bool Supports(string deliveryChannel) => "SMS".Equals(deliveryChannel, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 2.9 System Responses
    /// Successful delivery response example: {"ok":{"3069XXXXXXXX":"YYYYYYYYY"}}
    /// Successful delivery for multiple phones example: {"ok":{"3069XXXXXXXX":"YYYYYYYYY","3069XXXXXXXX":"YYYYYYYYY","3069XXXXXXXX":"YYYYYYYYY"}}
    /// Error response example: {"error":"Err315:Text was not provided"}
    /// </summary>
    internal class YubotoResponse
    {
        public Dictionary<string, string> OK { get; set; } = new Dictionary<string, string>();
        public string Error { get; set; }
        public bool HasError => !string.IsNullOrWhiteSpace(Error);
        public override string ToString() => HasError ? Error : $"{OK}";
    }

    internal class YubotoRequestHelper
    {
        public YubotoRequestHelper(string apiKey, string from, IEnumerable<string> to, string message, string action = "Send") {
            ApiKey = apiKey;
            Action = action;
            From = from;
            To = to;
            Message = message;
        }

        string ApiKey { get; set; }
        string Action { get; set; } = "Send";
        /// <summary>Numeric (maximum number of digits: 16) or alphanumeric characters (maximum number of characters: 11).</summary>
        string From { get; set; }
        /// <summary>Use country code without + or 00.</summary>
        IEnumerable<string> To { get; set; }
        /// <summary>
        /// Through Yuboto platform, you can send:
        ///     - Simple SMS(up to 160 characters)
        ///     - Flash SMS(up to 160 characters)
        ///     - Long SMS(more than 160 characters)
        ///     - Unicode SMS(up to 70 characters)
        /// Some 8bit alphabet characters may also be included and sent as a simple SMS. These will count as 2 characters. ^{}\[]~|€
        /// All the other characters included in the 8bit alphabet can only be sent as Unicode characters (SMS 70 characters).
        /// If you use small case Greek characters (8bit) in a non Unicode format, then the system will automatically convert them into Capital Greek characters (7bit).
        /// Long SMS is a text message longer than 160 characters. If the user’s mobile phone supports it, then the text message will be received as one. 
        /// Otherwise the message will be divided into multiple messages of 153 characters each (Maximum number of characters 2000).
        /// If you choose to send a long SMS without previously notifying the system, then the system will limit it to 160 characters(simple SMS).
        /// </summary>
        string Message { get; set; }
        /// <summary>Params: sms (default) / flash / unicode</summary>
        public string SmsType { get; set; } = "sms";
        /// <summary>Params: no (default) / yes</summary>
        public bool IsLongSms { get; set; }
        /// <summary>YYYYMMDD</summary>
        public string DateToSend { get; set; }
        /// <summary>HHMM</summary>
        public string TimeToSend { get; set; }
        /// <summary>Min 30, max 4320 (default).</summary>
        public int SmsValidity { get; set; } = 4320;
        public bool IsSmsTest { get; set; }
        public string CallbackUrl { get; set; }
        public string Option1 { get; set; }
        public string Option2 { get; set; }
        public bool IsJson { get; set; } = true;

        // https://services.yuboto.com/web2sms/api/v2/smsc.aspx?api_key=XXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXX&action=send&from=Grpm&to=306942012052&text=This%20is%20my%20first%20test
        public string CreateRequest() {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"?api_key={ApiKey}");
            stringBuilder.Append($"&action={Action}");
            stringBuilder.Append($"&from={From}");
            stringBuilder.Append($"&to={string.Join(",", To)}");
            stringBuilder.Append($"&text={Message}");
            var request = stringBuilder.ToString();
            return request;
        }
    }
}
