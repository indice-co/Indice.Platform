using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Indice.Services.Tests;

[Trait("Services", "SendGrid")]
public class SendGridServiceTests
{
    [Fact]
    public async Task SendAsync_Succeeds() {
        // Arrange
        var mockSettings = new Mock<IOptionsSnapshot<EmailServiceSendGridSettings>>();
        var mockHtmlRenderingEngine = new Mock<IHtmlRenderingEngine>();

        mockSettings.Setup(x => x.Value)
            .Returns(new EmailServiceSendGridSettings {
                Sender = "noreply@indice.gr",
                SenderName = "INDICE",
                ApiKey = Guid.NewGuid().ToString()
            });


        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock
            .Protected() // <= here is the trick to set up protected!!!
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage());

        var service = new EmailServiceSendGrid(
            mockSettings.Object,
            new HttpClient(httpMessageHandlerMock.Object),
            mockHtmlRenderingEngine.Object
        );

        // Act
        await service.SendAsync(new[] { "user@indice.gr" }, "Email Subject", "This is the body");

        // Assert
        Assert.True(true); // actually, no exception means that the test has passed
    }
}
