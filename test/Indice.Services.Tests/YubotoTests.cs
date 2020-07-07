using System;
using System.Net.Http;
using System.Threading.Tasks;
using Indice.Services.Yuboto;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Indice.Services.Tests
{
    public class YubotoTests
    {
        [Theory(Skip = "Sensitive Data")]
        [InlineData("", "", "Sender", "SenderName", "Test Subject", "Test Body")]
        public async Task TestSms(string apiKey, string phoneNumber, string sender, string senderName, string subject, string body) {
            var excepion = default(Exception);
            try {
                var service = new SmsYubotoOmniService(
                                    new HttpClient(),
                                    new SmsServiceSettings {
                                        ApiKey = apiKey,
                                        Sender = sender,
                                        SenderName = senderName,
                                        TestMode = true
                                    },
                                    new NullLogger<SmsYubotoOmniService>());
                await service.SendAsync(phoneNumber, subject, body);
            } catch (Exception smsServiceException) {
                excepion = smsServiceException;
            }
            Assert.Null(excepion);
        }
    }
}
