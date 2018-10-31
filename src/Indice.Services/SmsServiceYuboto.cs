using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Indice.Services
{
    /// <summary>
    /// Sms Service implementation using the Yuboto sms service gateway.
    /// </summary>
    public class SmsServiceYuboto : ISmsService, IDisposable
    {
        /// <summary>
        /// The settings required to configure the service
        /// </summary>
        protected SmsServiceSettings Settings { get; }
        HttpClient _http;

        /// <summary>
        /// Constructs the <see cref="SmsServiceYuboto"/> using the <seealso cref="SmsServiceSettings"/>
        /// </summary>
        /// <param name="settings"></param>
        public SmsServiceYuboto(SmsServiceSettings settings) {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _http = new HttpClient();
            var uri = "https://services.yuboto.com/web2sms/api/v2/smsc.aspx";
            _http.BaseAddress = new Uri(uri);
            //_http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(_Settings.ApiKey);
        }

        /// <summary>
        /// Send an sms to a recipient
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public async Task SendAsync(string destination, string subject, string body) {
            HttpResponseMessage httpResponse;
            YubotoResponse response;
            var recipients = (destination ?? "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            if (recipients == null)
                throw new ArgumentNullException(nameof(recipients));
            if (recipients.Length == 0)
                throw new ArgumentException("Recipients list is empty", nameof(recipients));
            if (recipients.Any(telephone => telephone.Any(telNumber => !char.IsNumber(telNumber))))
                throw new ArgumentException("Invalid recipients. Recipients cannot contain letters.", nameof(recipients));

            var request = new YubotoRequestHelper(Settings.ApiKey, (Settings.Sender ?? Settings.SenderName), recipients, body) {
#if DEBUG
                IsSmsTest = true,
#endif
            }.CreateStringRequest();

            httpResponse = await _http.GetAsync(request);
            var stringifyResponse = await httpResponse.Content.ReadAsStringAsync();
            response = JsonConvert.DeserializeObject<YubotoResponse>(stringifyResponse);
            if (response.HasError) {
                throw new SmsServiceException($"SMS Delivery failed. {response}");
            }
        }

        /// <summary>
        /// disposes the http client
        /// </summary>
        public void Dispose() {
            _http.Dispose();
        }

    }

    /// <summary>
    /// 2.9 System Responses
    /// Successful delivery response example:
    ///      {"ok":{"3069XXXXXXXX":"YYYYYYYYY"}}
    /// Successful delivery for multiple phones example:
    ///      {"ok":{"3069XXXXXXXX":"YYYYYYYYY","3069XXXXXXXX":"YYYYYYYYY","3069XXXXXXXX":"YYYYYYYYY"}}
    /// Error response example:
    ///      {"error":"Err315:Text was not provided"}
    /// </summary>
    internal class YubotoResponse
    {
        public Dictionary<string, string> OK { get; set; } = new Dictionary<string, string>();

        public string Error { get; set; }

        public bool HasError {
            get {
                return !string.IsNullOrWhiteSpace(Error);
            }
        }
        public override string ToString() {
            return HasError ? Error : $"{OK}";
        }
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

        /// <summary>
        /// Numeric (maximum number of digits: 16) or alphanumeric characters (maximum number of characters: 11)
        /// </summary>
        string From { get; set; }

        /// <summary>
        /// Use country code without + or 00.
        /// </summary>
        IEnumerable<string> To { get; set; }

        /// <summary>
        /// Through Yuboto platform, you can send:
        ///      Simple SMS(up to 160 characters)
        ///      Flash SMS(up to 160 characters)
        ///      Long SMS(more than 160 characters)
        ///      Unicode SMS(up to 70 characters)
        /// Some 8bit alphabet characters may also be included and sent as a simple SMS. These will count as 2 characters. ^{}\[]~|€
        /// All the other characters included in the 8bit alphabet can only be sent as Unicode characters (SMS 70 characters).
        /// If you use small case Greek characters (8bit) in a non Unicode format, then the system will automatically convert them into Capital Greek characters (7bit).
        /// Long SMS is a text message longer than 160 characters. If the user’s mobile phone supports it, then the text message will be received as one. 
        /// Otherwise the message will be divided into multiple messages of 153 characters each (Maximum number of characters 2000).
        /// If you choose to send a long SMS without previously notifying the system, then the system will limit it to 160 characters(simple SMS).
        /// </summary>
        string Message { get; set; }

        /// <summary>
        /// params: sms (default) / flash / unicode
        /// </summary>
        public string SmsType { get; set; } = "sms";

        /// <summary>
        /// params: no (default) / yes
        /// </summary>
        public bool IsLongSms { get; set; }

        /// <summary>
        /// YYYYMMDD
        /// </summary>
        public string DateToSend { get; set; }

        /// <summary>
        /// HHMM
        /// </summary>
        public string TimeToSend { get; set; }

        /// <summary>
        /// min 30
        /// max 4320 (default)
        /// </summary>
        public int SmsValidity { get; set; } = 4320;
        public bool IsSmsTest { get; set; }
        public string CallbackUrl { get; set; }
        public string Option1 { get; set; }
        public string Option2 { get; set; }
        public bool IsJson { get; set; } = true;

        public object CreateJsonObjRequest() {
            var requestObj = new {
                //Required
                api_key = ApiKey,
                action = Action,
                from = From,
                to = string.Join(",", To),
                text = Message,
                //Optional
                //typesms =           !string.IsNullOrWhiteSpace(SmsType) ? SmsType : null,
                //longsms =           IsLongSms ? "yes" : "no",
                //datein_to_send =    !string.IsNullOrWhiteSpace(DateToSend) ? DateToSend : null,
                //timein_to_send =    !string.IsNullOrWhiteSpace(TimeToSend) ? TimeToSend : null,
                //smsValidity =       SmsValidity,
                smstest = IsSmsTest ? "yes" : "no",
                //callback_url =      !string.IsNullOrWhiteSpace(CallbackUrl) ? CallbackUrl : null,
                //option1 =           !string.IsNullOrWhiteSpace(Option1) ? Option1 : null,
                //option2 =           !string.IsNullOrWhiteSpace(Option2) ? Option2 : null,
                //json =              IsJson ? "yes" : "no"
            };
            return requestObj;
        }

        //https://services.yuboto.com/web2sms/api/v2/smsc.aspx?api_key=XXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXX&action=send&from=Grpm&to=306942012052&text=This%20is%20my%20first%20test
        public string CreateStringRequest() {
            var sb = new System.Text.StringBuilder();

            sb.Append($"?api_key={ApiKey}");
            sb.Append($"&action={Action}");
            sb.Append($"&from={From}");
            sb.Append($"&to={string.Join(",", To)}");
            sb.Append($"&text={Message}");
            if (IsSmsTest) {
                //sb.Append($"&smstest=yes");
            }
            var request = sb.ToString();
            return request;
        }

    }
}
