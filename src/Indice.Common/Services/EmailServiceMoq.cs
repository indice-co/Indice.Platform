using System;
using System.Threading.Tasks;
using Indice.Configuration;

namespace Indice.Services
{
    public class EmailServiceMoq : IEmailService
    {
        public async Task SendAsync(string[] recipients, string subject, string body, FileAttachment[] attachments = null) => await SendAsync<object>(recipients, subject, body, null, null);

        public Task SendAsync<TModel>(string[] recipients, string subject, string body, string template, TModel data, FileAttachment[] attachments = null) where TModel : class {
            foreach (var recipient in recipients) {
                Console.WriteLine($"Email:\n\t\t{recipient}/{subject}\n\n\t\t{body}");
            }
#if NET452
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        public Task SendAsync<TModel>(Action<EmailMessageBuilder<TModel>> configureMessage) where TModel : class {
            throw new NotImplementedException();
        }

        public Task SendAsync(Action<EmailMessageBuilder> configureMessage) {
            throw new NotImplementedException();
        }
    }
}
