using Microsoft.Extensions.Options;
using Moq.Protected;
using Moq;
using Xunit;

namespace Indice.Services.Tests;

[Trait("Services", "Brevo")]
public class BrevoServiceTests
{
    [Fact]
    public async Task SendAsync_Succeeds() {
        // Arrange
        var mockSettings = new Mock<IOptionsSnapshot<EmailServiceBrevoSettings>>();
        var mockHtmlRenderingEngine = new Mock<IHtmlRenderingEngine>();

        mockSettings.Setup(x => x.Value)
            .Returns(new EmailServiceBrevoSettings {
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

        var service = new EmailServiceBrevo(
            mockSettings.Object,
            new HttpClient(httpMessageHandlerMock.Object),
            mockHtmlRenderingEngine.Object
        );

        // Act
        await service.SendAsync([ "user@indice.gr" ], "Email Subject", "This is the body");

        // Assert
        Assert.True(true); // actually, no exception means that the test has passed
    }
}
