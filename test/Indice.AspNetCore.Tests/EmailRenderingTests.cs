using Indice.Services;
using Moq;
using Xunit;

namespace Indice.AspNetCore.Tests
{
    public class EmailRenderingTests
    {
        public EmailRenderingTests() {
            TestServer = new TestServerFixture();
        }

        private TestServerFixture TestServer { get; set; }

        [Fact]
        public async Task Can_Render_Simple_Template() {
            var renderingEngine = TestServer.GetRequiredService<IHtmlRenderingEngine>();
            Assert.IsType<HtmlRenderingEngineMvcRazor>(renderingEngine);
            var model = new EmailModel {
                Salutation = "Mr.",
                FullName = "Georgios",
                Message = "Good for you to write some tests. Be a man!"
            };
            var output = await renderingEngine.RenderAsync("Simple", model);
            var outputFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Outputs\\simple.html");
            if (!File.Exists(outputFilePath)) {
                throw new FileNotFoundException($"Output file '{outputFilePath}' does not exist");
            }
            var expectedOutput = await File.ReadAllTextAsync(outputFilePath);
            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public async Task Can_Send_Email_With_Simple_Template() {
            var emailServiceMock = new Mock<IEmailService>();
            var renderingEngine = TestServer.GetRequiredService<IHtmlRenderingEngine>();
            emailServiceMock.Setup(x => x.HtmlRenderingEngine).Returns(renderingEngine);
            await emailServiceMock.Object.SendAsync(messageBuilder => messageBuilder
                .To("g.manoltzas@indice.gr")
                .WithSubject("Verification")
                .WithData(new EmailModel {
                    Salutation = "Mr.",
                    FullName = "Georgios",
                    Message = "Good for you to write some tests. Be a man!"
                })
                .UsingTemplate("Simple")
            );
            var outputFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Outputs\\simple.html");
            if (!File.Exists(outputFilePath)) {
                throw new FileNotFoundException($"Output file '{outputFilePath}' does not exist");
            }
            var expectedBody = await File.ReadAllTextAsync(outputFilePath);
            emailServiceMock.Verify(p => p.SendAsync(
                It.Is<string[]>(recipients => recipients[0] == "g.manoltzas@indice.gr"),
                It.Is<string>(subject => subject == "Verification"),
                It.Is<string>(body => body == expectedBody),
                It.Is<EmailAttachment[]>(attachments => attachments.Count() == 0)
            ), Times.Once);
        }
    }

    public class EmailModel
    {
        public string Salutation { get; set; }
        public string FullName { get; set; }
        public string Message { get; set; }
    }
}
