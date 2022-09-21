using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Indice.Services.Tests
{
    public class PushNotificationMessageBuilderTests
    {
        [Fact]
        public async Task Can_Build_Push_Notification_Message() {
            // Arrange
            var pushNotificationService = new Mock<IPushNotificationService>();
            var pushNotificationBuilder = new PushNotificationMessageBuilder()
                .To("5372ef3e-9bf8-464d-8fc9-3234a2b979f6")
                .WithTitle("This is the title!")
                .WithBody("This is the body!")
                .WithData("{\"connectionId\":\"1234-ab-cd\", \"otp\":123456}")
                .WithClassification("Approvals")
                .WithTags("tag-1", "tag-2");
            // Act
            await pushNotificationService.Object.SendAsync(_ => pushNotificationBuilder);
            // Assert
            pushNotificationService.Verify(push => push.SendAsync(
                It.Is<string>(title => title == "This is the title!"),
                It.Is<string>(body => body == "This is the body!"),
                It.Is<IList<string>>(tags => tags.ElementAt(0) == "5372ef3e-9bf8-464d-8fc9-3234a2b979f6" && tags.ElementAt(1) == "tag-1" && tags.ElementAt(2) == "tag-2"),
                It.Is<string>(dataJson => dataJson == "{\"connectionId\":\"1234-ab-cd\", \"otp\":123456}"),
                It.Is<string>(classification => classification == "Approvals")
            ), Times.Once);
        }
    }
}