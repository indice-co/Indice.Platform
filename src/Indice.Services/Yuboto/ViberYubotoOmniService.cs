using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Indice.Services.Yuboto.Bases;
using Microsoft.Extensions.Logging;

namespace Indice.Services.Yuboto
{
    /// <summary>
    /// Service to send SMS via Viber using Yuboto API
    /// </summary>
    public class ViberYubotoOmniService : YubotoOmniServiceBase, ISmsService
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="settings"></param>
        /// <param name="logger"></param>
        public ViberYubotoOmniService(HttpClient httpClient, SmsServiceSettings settings, ILogger<ViberYubotoOmniService> logger) : base(httpClient, settings, logger) { }

        /// <inheritdoc />
        public async Task SendAsync(string destination, string subject, string body) {
            var phoneNumbers = GetRecipientsFromDestination(destination);
            var requestBody = SendRequest.CreateViber(phoneNumbers, Settings.Sender ?? Settings.SenderName, body);
            var jsonData = JsonSerializer.Serialize(requestBody, GetJsonSerializerOptions());
            Logger.LogInformation("The following payload was sent to Yuboto: {0}", jsonData);
            var data = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var httpResponseMessage = await HttpClient.PostAsync("Send", data);
            if (!httpResponseMessage.IsSuccessStatusCode) {
                var errorMessage = "SMS Delivery failed.\n";
                if (httpResponseMessage.Content != null) {
                    errorMessage += await httpResponseMessage.Content.ReadAsStringAsync();
                }
                Logger.LogError(errorMessage);
                throw new SmsServiceException(errorMessage);
            }
            var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();
            Logger.LogInformation("The following response was received from Yuboto: {0}", responseContent);
            var response = JsonSerializer.Deserialize<SendResponse>(responseContent);
            if (!response.IsSuccess) {
                var errorMessage = $"SMS Delivery failed: {response.ErrorCode} - {response.ErrorMessage}";
                Logger.LogError(errorMessage);
                throw new SmsServiceException(errorMessage);
            }
            Logger.LogInformation("SMS message successfully sent.");
        }

        /// <inheritdoc />
        public bool Supports(string deliveryChannel) => "Viber".Equals(deliveryChannel, StringComparison.OrdinalIgnoreCase);
    }
}
