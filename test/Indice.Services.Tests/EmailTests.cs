using System.Threading.Tasks;
using Xunit;

namespace Indice.Services.Tests
{
    public class EmailTests
    {
        [Fact]
        public async Task EmailBuilderTest () {
            IEmailService emailService = new EmailServiceNoop();
            await emailService.SendAsync(builder => 
                builder.WithSubject("This will blow your mind")
                       .WithBody("This is a strong <strong>body</strong>.")
                       .To("c.leftheris@indice.gr")
                       .UsingTemplate("MyFantasticTemplate")
                       .WithData(new {
                           FirstName = "Constantinos",
                           LastName = "Leftheris"
                       })
            );
            Assert.True(true);

        }

        [Fact]
        public async Task EmailBuilderTestNoBody() {
            IEmailService emailService = new EmailServiceNoop();
            await emailService.SendAsync(builder => 
                builder.WithSubject("This will blow your mind")
                       .To("c.leftheris@indice.gr")
                       .UsingTemplate("MyFantasticTemplate")
                       .WithData(new {
                           FirstName = "Constantinos",
                           LastName = "Leftheris"
                       })
            );
            Assert.True(true);
        }
    }
}
