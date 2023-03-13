using Indice.Types;
using Moq;
using Xunit;

namespace Indice.Services.Tests;

public class PushNotificationMessageBuilderTests
{
    [Fact]
    public async Task Can_Build_Push_Notification_Message() {
        // Arrange
        var pushNotificationService = new Mock<IPushNotificationService>();
        // Act
        await pushNotificationService.Object.SendAsync(message => message
            .ToUser("5372ef3e-9bf8-464d-8fc9-3234a2b979f6")
            .WithTitle("This is the title!")
            .WithBody("This is the body!")
            .WithData(new {
                ConnectionId = "1234-ab-cd",
                Otp = 123456
            })
            .WithClassification("Approvals")
            .WithTags("tag-1", "tag-2")
        );
        // Assert
        pushNotificationService.Verify(push => push.SendAsync(
            It.Is<string>(title => title == "This is the title!"),
            It.Is<string>(body => body == "This is the body!"),
            It.Is<IList<PushNotificationTag>>(tags =>
                tags.ElementAt(0) == new PushNotificationTag("tag-1", PushNotificationTagKind.Unspecified) &&
                tags.ElementAt(1) == new PushNotificationTag("tag-2", PushNotificationTagKind.Unspecified) &&
                tags.ElementAt(2) == new PushNotificationTag("5372ef3e-9bf8-464d-8fc9-3234a2b979f6", PushNotificationTagKind.User)
            ),
            It.Is<string>(dataJson => dataJson == "{\"connectionId\":\"1234-ab-cd\",\"otp\":123456}"),
            It.Is<string>(classification => classification == "Approvals")
        ), Times.Once);
    }

    [Fact]
    public void Can_Parse_Valid_User_Push_Notification_Tag() {
        var tagString = "user:0FC97737-D65D-48FC-AB38-5AA2E9853E5C";
        var tag = PushNotificationTag.Parse(tagString);
        Assert.True(tag.Kind == PushNotificationTagKind.User);
        Assert.True(tag.Value == "0FC97737-D65D-48FC-AB38-5AA2E9853E5C");
    }

    [Fact]
    public void Can_Parse_Valid_Unspecified_Push_Notification_Tag() {
        var tagString = "0FC97737-D65D-48FC-AB38-5AA2E9853E5C";
        var tag = PushNotificationTag.Parse(tagString);
        Assert.True(tag.Kind == PushNotificationTagKind.Unspecified);
        Assert.True(tag.Value == "0FC97737-D65D-48FC-AB38-5AA2E9853E5C");
    }
}