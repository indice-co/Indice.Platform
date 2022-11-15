//using System.Linq;
//using System.Security.Claims;
//using System.Text.Json;
//using System.Threading.Tasks;
//using Indice.Security;
//using Indice.Serialization;
//using Moq;
//using Xunit;

//namespace Indice.Services.Tests
//{
//    public class TotpMessageBuilderTests
//    {
//        [Fact]
//        public async Task Can_Build_Simple_Totp_Message() {
//            const string userId = "6A870D4F-CCB9-441A-AB2F-76306F2F5B2E";
//            // Arrange
//            var totpService = new Mock<ITotpService>();
//            // Act
//            await totpService.Object.Send(builder => {
//                builder.UsePrincipal(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(BasicClaimTypes.Subject, userId) })))
//                       .WithMessage("Your TOTP is {0}")
//                       .UsingSms()
//                       .WithPurpose("123abc")
//                       .WithData(new TotpData { Name = "John" });
//            });
//            // Assert
//            totpService.Verify(totp => totp.Send(
//                It.Is<ClaimsPrincipal>(principal => principal.Claims.Single(x => x.Type == BasicClaimTypes.Subject).Value.Equals(userId)),
//                It.Is<string>(message => message == "Your TOTP is {0}"),
//                It.Is<TotpDeliveryChannel>(channel => channel == TotpDeliveryChannel.Sms),
//                It.Is<string>(purpose => purpose == "123abc"),
//                null,
//                null,
//                It.Is<string>(data => JsonSerializer.Deserialize<TotpData>(data, JsonSerializerOptionDefaults.GetDefaultSettings()).Name == "John"),
//                null,
//                null
//            ), Times.Once);
//        }

//        private class TotpData
//        {
//            public string Name { get; set; }
//        }
//    }
//}
