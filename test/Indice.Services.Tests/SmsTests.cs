using Indice.Services.Yuboto;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Indice.Services.Tests;

public class SmsTests
{
    [Theory(Skip = "Sensitive Data")]
    [InlineData("", "", "Sender", "SenderName", "Test Subject", "Test Body")]
    public async Task TestYubotoOmniSms(string apiKey, string phoneNumber, string sender, string senderName, string subject, string body) {
        var exception = default(Exception);
        try {
            var service = new SmsYubotoOmniService(
                new HttpClient(),
                Options.Create(new SmsServiceSettings {
                    ApiKey = apiKey,
                    Sender = sender,
                    SenderName = senderName,
                    TestMode = true
                }) as IOptionsSnapshot<SmsServiceSettings>,
                new NullLogger<SmsYubotoOmniService>()
         );
            await service.SendAsync(phoneNumber, subject, body);
        } catch (Exception smsServiceException) {
            exception = smsServiceException;
        }
        Assert.Null(exception);
    }

    [Theory(Skip = "Sensitive Data")]
    [InlineData("", "", "", "Indice", "Indice", "Test Subject", "Test Body")]
    public async Task TestApifonSms(string apiKey, string token, string phoneNumber, string sender, string senderName, string subject, string body) {
        var excepion = default(Exception);
        try {
            var service = new SmsServiceApifon(
                new HttpClient { BaseAddress = new Uri("https://ars.apifon.com/services/api/v1/sms/") },
                Options.Create(new SmsServiceApifonSettings {
                    ApiKey = apiKey,
                    Token = token,
                    Sender = sender,
                    SenderName = senderName,
                    TestMode = true
                }) as IOptionsSnapshot<SmsServiceApifonSettings>,
                new NullLogger<SmsServiceApifon>()
            );
            await service.SendAsync(phoneNumber, subject, body);
        } catch (Exception smsServiceException) {
            excepion = smsServiceException;
        }
        Assert.Null(excepion);
    }
}
