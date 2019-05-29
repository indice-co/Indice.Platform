using System;
using System.Threading.Tasks;
using Indice.Configuration;

namespace Indice.Services
{
    /// <summary>
    /// Moq implementation of <see cref="IEmailService"/>.
    /// </summary>
    public class EmailServiceMoq : IEmailService
    {
        /// <inheritdoc/>
        public async Task SendAsync(string[] recipients, string subject, string body, FileAttachment[] attachments = null) => await SendAsync<object>(recipients, subject, body, null, null);

        /// <inheritdoc/>
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
    }
}
