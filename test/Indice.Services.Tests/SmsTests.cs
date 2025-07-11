using Indice.Services.Yuboto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Indice.Services.Tests;

public class SmsTests
{
    [Theory(Skip = "Sensitive Data")]
    [InlineData("", "", "Sender", "SenderName", "Test Subject", "Test Body")]
    public async Task TestYubotoOmniSms(string apiKey, string phoneNumber, string sender, string senderName, string subject, string body) {
        var exception = default(Exception);
        try {
            var optionsSnapshotMock = new Mock<IOptionsSnapshot<SmsServiceSettings>>();
            optionsSnapshotMock.Setup(x => x.Value).Returns(new SmsServiceSettings {
                ApiKey = apiKey,
                Sender = sender,
                SenderName = senderName,
                TestMode = true
            });
            var service = new SmsYubotoOmniService(
                new HttpClient(),
                optionsSnapshotMock.Object,
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
    [InlineData("", "", "", "Indice", "Indice", "Test Subject", "Welcome to https://www.indice.gr! You can manage you account here: https://my.indice.com.")]
    public async Task TestApifonSms(string apiKey, string token, string phoneNumber, string sender, string senderName, string subject, string body) {
        var inMemorySettings = new Dictionary<string, string> {
            ["Sms:ApiKey"] = apiKey,
            ["Sms:Token"] = token,
            ["Sms:Sender"] = sender,
            ["Sms:SenderName"] = senderName,
            ["Sms:TestMode"] = true.ToString(),
            ["Sms:EnableUrlShortener"] = true.ToString(),
            ["Sms:WebhookUrl"] = string.Empty,
            ["Sms:WebhookSecret"] = string.Empty
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        var collection = new ServiceCollection()
            .AddSingleton(configuration)
            .AddOptions()
            .Configure<SmsServiceApifonSettings>(configuration.GetSection(SmsServiceApifonSettings.Name))
            .AddSmsServiceApifon(configuration);


        var serviceProvider = collection.BuildServiceProvider();
        var exception = default(Exception);

        try {
            var service = serviceProvider.GetRequiredService<ISmsService>();
            await service.SendAsync(phoneNumber, subject, body);
        } catch (Exception smsServiceException) {
            exception = smsServiceException;
        }
        Assert.Null(exception);
    }

    [Theory(Skip = "Sensitive Data")]
    [InlineData("", "", "", "Indice", "Indice", "Test from ApifonIM", "Apifon Viber message body")]
    [InlineData("", "", "", "Indice", "Indice", "Test from ApifonIM", "Welcome to https://www.indice.gr! You can manage you account here: https://my.indice.com.")]
    public async Task TestApifonIM(string apiKey, string token, string phoneNumber, string sender, string senderName, string subject, string body) {
        var inMemorySettings = new Dictionary<string, string> {
            ["Sms:ApiKey"] = apiKey,
            ["Sms:Token"] = token,
            ["Sms:Sender"] = sender,
            ["Sms:SenderName"] = senderName,
            ["Sms:TestMode"] = true.ToString(),
            ["Sms:EnableUrlShortener"] = true.ToString()
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        var collection = new ServiceCollection()
            .AddSingleton(configuration)
            .AddOptions()
            .Configure<SmsServiceApifonSettings>(configuration.GetSection(SmsServiceApifonSettings.Name))
            .AddSmsServiceApifonIM(configuration);

        var serviceProvider = collection.BuildServiceProvider();
        var error = default(Exception);

        try {
            var service = serviceProvider.GetRequiredService<ISmsService>();
            await service.SendAsync(phoneNumber, subject, body);
        } catch (Exception smsServiceException) {
            error = smsServiceException;
        }
        Assert.Null(error);
    }

    [Theory(Skip = "Sensitive Data")]
    [InlineData("", "Hello from INDICE", "", "", "Indice")]
    public async Task TestVonageSms(string phoneNumber, string body, string apiKey, string signatureSecret, string sender) {
        var inMemorySettings = new Dictionary<string, string> {
            ["Sms:ApiKey"] = apiKey,
            ["Sms:SignatureSecret"] = signatureSecret,
            ["Sms:Sender"] = sender,
            ["Sms:SenderName"] = sender,
            ["Sms:TestMode"] = true.ToString(),
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var collection = new ServiceCollection()
          .AddSingleton(configuration)
          .AddOptions()
          .Configure<SmsServiceVonageSettings>(configuration.GetSection(SmsServiceVonageSettings.Name))
          .AddSmsServiceVonage(configuration);

        var serviceProvider = collection.BuildServiceProvider();
        var error = default(Exception);

        try {
            var service = serviceProvider.GetRequiredService<ISmsService>();
            await service.SendAsync(phoneNumber, "subject", body);
        } catch (Exception smsServiceException) {
            error = smsServiceException;
        }
        Assert.Null(error);
    }

    [Theory(Skip = "Sensitive Data")]
    [InlineData("", "Hello from INDICE", "", "", "", "", "", "")]
    public async Task TestTwilioSms(string phoneNumber, string body, string accountSid, string apiKey, string secret, string sender, string authToken, string messagingServiceSid) {
        
        var inMemorySettings = new Dictionary<string, string> {
            ["Sms:AccountSid"] = accountSid
        };
        // Use MessagingServiceSid if present; otherwise use From number
        if (!string.IsNullOrWhiteSpace(messagingServiceSid)) {
            inMemorySettings["Sms:MessagingServiceSid"] = messagingServiceSid;
        } else {
            inMemorySettings["Sms:SenderPhoneNumber"] = sender;
        }
        // Use Secret and ApiKey if present; otherwise use AuthToken number
        if (!string.IsNullOrWhiteSpace(secret) && !string.IsNullOrWhiteSpace(apiKey)) {
            inMemorySettings["Sms:ApiKey"] = apiKey;
            inMemorySettings["Sms:Secret"] = secret;
        } else {
            inMemorySettings["Sms:AuthToken"] = authToken;
        }
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var collection = new ServiceCollection()
          .AddSingleton(configuration)
          .AddOptions()
          .Configure<SmsServiceTwilioSettings>(configuration.GetSection(SmsServiceTwilioSettings.Name))
          .AddSmsServiceTwilio(configuration);

        var serviceProvider = collection.BuildServiceProvider();
        var error = default(Exception);

        try {
            var service = serviceProvider.GetRequiredService<ISmsService>();
            await service.SendAsync(phoneNumber, "subject", body);
        } catch (Exception smsServiceException) {
            error = smsServiceException;
        }
        Assert.Null(error);
    }

    [Theory(Skip = "Sensitive Data")]
    [InlineData("", "", "", "Test Subject", "Test Body", "Test")]
    public async Task TestMstatSms(string apiToken, string phoneNumber, string sender, string subject, string body, string senderName) {

        var inMemorySettings = new Dictionary<string, string> {
            ["Sms:ApiKey"] = apiToken,
            ["Sms:Sender"] = sender,
            ["Sms:SenderName"] = senderName
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        var collection = new ServiceCollection()
            .AddSingleton(configuration)
            .AddOptions()
            .Configure<SmsServiceMstatSettings>(configuration.GetSection(SmsServiceMstatSettings.Name))
            .AddSmsServiceMstat(configuration);

        var serviceProvider = collection.BuildServiceProvider();
        var excepion = default(Exception);
        try {
            var service = serviceProvider.GetRequiredService<ISmsService>();
            await service.SendAsync(phoneNumber, subject, body);
        } catch (Exception smsServiceException) {
            excepion = smsServiceException;
        }
        Assert.Null(excepion);
    }
}
