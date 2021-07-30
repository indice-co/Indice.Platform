using System;
using System.Threading.Tasks;
using Xunit;

namespace Indice.Services.Tests
{
    public class PushNotificationMessageTests
    {
        [Fact]
        public async Task PushNotificationBuilderTestMinimum() {
            IPushNotificationService service = new MockPushNotificationService();

            await service.SendAsync(builder => builder
                .To(Guid.NewGuid().ToString())
                .WithMessage("Push Notification Message")
                .WithToken("123456"));

            Assert.True(true);
        }

        [Fact]
        public async Task PushNotificationBuilderTestWithData() {
            IPushNotificationService service = new MockPushNotificationService();

            await service.SendAsync(builder => builder
                .To(Guid.NewGuid().ToString())
                .WithMessage("Push Notification Message")
                .WithToken("123456")
                .WithData("{{\"connectionId\":\"1234-abcd\", \"otp\":{0}}}"));

            Assert.True(true);
        }

        [Fact]
        public async Task PushNotificationBuilderTestAllOptions() {
            IPushNotificationService service = new MockPushNotificationService();

            await service.SendAsync(builder => builder
                .To(Guid.NewGuid().ToString())
                .WithMessage("Push Notification Message")
                .WithToken("123456")
                .WithData("{{\"connectionId\":\"1234-abcd\", \"otp\":{0}}}")
                .WithTags("tag1", "tag2"));

            Assert.True(true);
        }
    }
}