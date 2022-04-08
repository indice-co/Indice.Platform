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
        public async Task Can() {
            var emailServiceMock = new Mock<IEmailService>();
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
        }
    }

    public class EmailModel
    {
        public string Salutation { get; set; }
        public string FullName { get; set; }
        public string Message { get; set; }
    }
}
