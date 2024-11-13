﻿using System.Text;
using System.Text.Json;
using Indice.Services.Yuboto.Bases;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Services.Yuboto;

/// <summary>Service to send classic SMS using Yuboto API.</summary>
public class SmsYubotoOmniService : YubotoOmniServiceBase, ISmsService
{
    /// <summary>Creates a new instance of <see cref="SmsYubotoOmniService"/>.</summary>
    /// <param name="httpClient">Provides a base class for sending HTTP requests and receiving HTTP responses from a resource identified by a URI.</param>
    /// <param name="settings">Settings class for configuring SMS service clients.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public SmsYubotoOmniService(
        HttpClient httpClient, 
        IOptionsSnapshot<SmsServiceSettings> settings, 
        ILogger<SmsYubotoOmniService> logger
    ) : base(httpClient, settings, logger) { }

    /// <inheritdoc />
    public async Task<SendReceipt> SendAsync(string destination, string subject, string body, SmsSender sender = null) {
        var messageId = Guid.NewGuid().ToString();
        var phoneNumbers = GetRecipientsFromDestination(destination);
        var requestBody = SendRequest.CreateSms(phoneNumbers, sender?.Id ?? Settings.Sender ?? Settings.SenderName, body, Settings.Validity);
        var jsonData = JsonSerializer.Serialize(requestBody, GetJsonSerializerOptions());
        var data = new StringContent(jsonData, Encoding.UTF8, "application/json");
        var httpResponseMessage = await HttpClient.PostAsync("Send", data);
        if (!httpResponseMessage.IsSuccessStatusCode) {
            var errorMessage = "SMS Delivery failed.\n ";
            if (httpResponseMessage.Content != null) {
                errorMessage += await httpResponseMessage.Content.ReadAsStringAsync();
            }
            Logger.LogError(errorMessage);
            throw new SmsServiceException(errorMessage);
        }
        var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<SendResponse>(responseContent);
        if (!response.IsSuccess) {
            var errorMessage = $"SMS Delivery failed.\n {response.ErrorCode} - {response.ErrorMessage}.\n {JsonSerializer.Serialize(response.Messages)}";
            Logger.LogError(errorMessage);
            throw new SmsServiceException(errorMessage);
        }
        Logger.LogInformation("SMS message successfully sent");
        if (response.Messages?.Count > 0) {
            messageId = string.Join(',', response.Messages.Select(x => x.Id));
        }
        return new SendReceipt(messageId, DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public bool Supports(string deliveryChannel) => "Sms".Equals(deliveryChannel, StringComparison.OrdinalIgnoreCase);
}
