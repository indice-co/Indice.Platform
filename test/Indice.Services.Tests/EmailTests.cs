using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Indice.Services.Tests
{
    public class EmailTests
    {
        [Fact]
        public async Task EmailBuilderTest () {
            IEmailService emailService = new EmailServiceMoq();
            await emailService.SendAsync(x => x.WithSubject("This will blow your mind")
                                         .WithBody("This is a strong <strong>body</strong>.")
                                         .To("c.leftheris@indice.gr")
                                         .UsingTemplate("MyFantasticTemplate")
                                         .WithData(new {
                                             FirstName = "Constantinos",
                                             LastName = "Leftheris"
                                         }));

            Assert.True(true);

        }
    }
}
