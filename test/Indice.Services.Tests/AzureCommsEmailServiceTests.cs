using System.Reflection;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using EmailMessageAzure=Azure.Communication.Email.EmailMessage;
namespace Indice.Services.Tests;

[Trait("Services", "Azure Comms Email Service")]
public sealed class AzureCommsEmailServiceTests
{
    private readonly Mock<IOptionsSnapshot<EmailServiceAzureCommsSettings>> _mockSettings;
    private readonly Mock<IHtmlRenderingEngine> _mockHtmlRenderingEngine;

    public AzureCommsEmailServiceTests()
    {
        _mockSettings = new Mock<IOptionsSnapshot<EmailServiceAzureCommsSettings>>();
        _mockSettings.Setup(x => x.Value)
            .Returns(new EmailServiceAzureCommsSettings {
                Sender = "noreply@indice.gr",
                ClientId = Guid.NewGuid().ToString(),
                ClientSecret = Guid.NewGuid().ToString(),
                TenantId = Guid.NewGuid().ToString(),
                ResourceEndpoint = $"https://{Guid.NewGuid()}.communication.azure.com/",
                WaitUntilCompleted = false
            });
        _mockHtmlRenderingEngine = new Mock<IHtmlRenderingEngine>();
    }

    [Fact]
    public async Task SendAsync_Succeeds() {
        var expectedOperationId = Guid.NewGuid().ToString();
        var mockOperation = new Mock<EmailSendOperation>();
        mockOperation.Setup(x => x.Id).Returns(expectedOperationId);

        var mockEmailClient = new Mock<EmailClient>();
        mockEmailClient
            .Setup(x => x.SendAsync(It.IsAny<WaitUntil>(), It.IsAny<EmailMessageAzure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockOperation.Object);

        var service = new AzureCommunicationServicesEmailService(_mockSettings.Object, _mockHtmlRenderingEngine.Object);

        // Using reflection swap the private _emailClient field with our mock
        var clientField = typeof(AzureCommunicationServicesEmailService).GetField(
            "_emailClient",
            BindingFlags.NonPublic | BindingFlags.Instance);

        clientField!.SetValue(service, mockEmailClient.Object);
        var receipt = await service.SendAsync([ "user@indice.gr" ], "Test Email Subject", "This is the test body");
        Assert.NotNull(receipt);
        Assert.Equal(expectedOperationId, receipt.MessageId);
    }
}
