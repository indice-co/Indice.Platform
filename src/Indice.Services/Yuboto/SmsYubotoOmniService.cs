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
    /// Service to send classic SMS using Yuboto API
    /// </summary>
    public class SmsYubotoOmniService : YubotoOmniServiceBase, ISmsService
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="settings"></param>
        /// <param name="logger"></param>
        public SmsYubotoOmniService(HttpClient httpClient, SmsServiceSettings settings, ILogger<SmsYubotoOmniService> logger)
            : base(httpClient, settings, logger) {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public async Task SendAsync(string destination, string subject, string body) {
            var phoneNumbers = GetRecipientsFromDestination(destination);
            var requestBody = SendRequest.CreateSms(phoneNumbers, Settings.Sender ?? Settings.SenderName, body);
            var jsonData = JsonSerializer.Serialize(requestBody);
            var data = new StringContent(jsonData, Encoding.UTF8, "application/json");

            var httpResponseMessage = await HttpClient.PostAsync($"Send", data);
            if (!httpResponseMessage.IsSuccessStatusCode) {
                var errorMessage = "SMS Delivery failed.\n ";
                if (httpResponseMessage.Content != null) {
                    errorMessage += await httpResponseMessage.Content.ReadAsStringAsync();
                }
                Logger?.LogError(errorMessage);
                throw new SmsServiceException(errorMessage);
            }

            var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();
            var response = JsonSerializer.Deserialize<SendResponse>(responseContent);
            if (!response.IsSuccess) {
                var errorMessage = $"SMS Delivery failed.\n {response.ErrorCode} - {response.ErrorMessage}.\n {JsonSerializer.Serialize(response.Messages)}";
                Logger?.LogError(errorMessage);
                throw new SmsServiceException(errorMessage);
            }

            Logger?.LogInformation("SMS message successfully sent: \n", JsonSerializer.Serialize(response.Messages));
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="deliveryChannel"></param>
        /// <returns></returns>
        public bool Supports(string deliveryChannel) => "Sms".Equals(deliveryChannel, StringComparison.OrdinalIgnoreCase);
    }
}
