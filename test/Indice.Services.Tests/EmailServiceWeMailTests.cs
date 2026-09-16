using Microsoft.Extensions.Options;
using Moq;

namespace Indice.Services.Tests;

[Trait("Services", "WeMail Email Service")]
public sealed class EmailServiceWeMailTests
{
    private Mock<IOptionsSnapshot<EmailServiceWeMailSettings>> _mockSettings;
    private Mock<IHtmlRenderingEngine> _mockHtmlRenderingEngine;

    public EmailServiceWeMailTests() { }

    [Theory(Skip = "Sensitive Data")]
    [InlineData("", "")]
    public async Task SendAsync_Succeeds(string apiKey, string toAddress) {
        _mockSettings = new Mock<IOptionsSnapshot<EmailServiceWeMailSettings>>();
        _mockSettings.Setup(x => x.Value)
            .Returns(new EmailServiceWeMailSettings {
                Sender = "noreply@indice.gr",
                SenderName = "Indice",
                ApiKey = apiKey
            });
        _mockHtmlRenderingEngine = new Mock<IHtmlRenderingEngine>();

        var expectedMessageId = Guid.NewGuid().ToString();

        using var httpClient = new HttpClient();

        var service = new EmailServiceWeMail(
            _mockSettings.Object,
            httpClient,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<EmailServiceWeMail>>(),
            _mockHtmlRenderingEngine.Object);

        var receipt = await service.SendAsync([toAddress], "Test Email Subject", "This is the test body");

        Assert.NotNull(receipt);
        Assert.Equal(expectedMessageId, receipt.MessageId);
    }
}