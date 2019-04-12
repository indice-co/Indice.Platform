using System.Threading.Tasks;
using Indice.Services;
using Xunit;

namespace Indice.Common.Tests
{
    public class EmailMessageBuilderTests
    {
        [Fact]
        public async Task CheckEmailMessageBuilderSyntax() {
            var emailService = new EmailServiceSmtpRazor(null, null, null, null, null);

            await emailService.SendAsync<SmsServiceSettings>(options =>
                options.AddRecipients("g.manoltzas@indice.gr", "c.leftheris@indice.gr")
                       .AddSubject("Email subject")
                       .AddBody("<h1>Hello there!</h1>")
                       .AddTemplate("Email")
                       .AddData(new SmsServiceSettings { }));
        }
    }
}
